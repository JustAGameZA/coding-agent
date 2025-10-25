"""Integration tests for hybrid classifier."""

import asyncio

import pytest
from api.schemas.classification import ClassificationRequest
from domain.classifiers.hybrid import (
    CircuitBreaker,
    ClassificationMetrics,
    HybridClassifier,
)
from domain.classifiers.heuristic import HeuristicClassifier
from domain.classifiers.llm_classifier import LLMClassifier
from domain.models.task_type import TaskComplexity, TaskType


@pytest.fixture
def hybrid_classifier():
    """Create a hybrid classifier instance for testing."""
    return HybridClassifier(enable_circuit_breaker=True)


@pytest.fixture
def hybrid_classifier_no_ml():
    """Create a hybrid classifier without ML (heuristic + LLM only)."""
    return HybridClassifier(
        heuristic=HeuristicClassifier(),
        ml_classifier=None,  # No ML classifier
        llm_classifier=LLMClassifier(),
        enable_circuit_breaker=True,
    )


class TestHybridClassifierPaths:
    """Test all three classification paths."""

    @pytest.mark.asyncio
    async def test_heuristic_path_high_confidence(self, hybrid_classifier):
        """Test that high-confidence heuristic results are used (Phase 1)."""
        # Create a request with clear bug fix keywords
        request = ClassificationRequest(
            task_description="Fix the critical login bug that causes authentication failures"
        )

        result = await hybrid_classifier.classify(request)

        # Should use heuristic path due to high confidence
        assert result.classifier_used == "heuristic"
        assert result.task_type == TaskType.BUG_FIX
        assert result.confidence >= HybridClassifier.HEURISTIC_THRESHOLD

        # Check metrics
        metrics = hybrid_classifier.get_metrics()
        assert metrics["heuristic_used"] == 1
        assert metrics["ml_used"] == 0
        assert metrics["llm_used"] == 0
        assert metrics["heuristic_percent"] == 100.0

    @pytest.mark.asyncio
    async def test_llm_fallback_path_no_ml(self, hybrid_classifier_no_ml):
        """Test LLM fallback when heuristic confidence is low and no ML (Phase 3)."""
        # Ambiguous request with low heuristic confidence
        request = ClassificationRequest(
            task_description="Do something with the user system"
        )

        result = await hybrid_classifier_no_ml.classify(request)

        # Should fall back to LLM due to low heuristic confidence and no ML
        assert result.classifier_used == "llm"
        # LLM provides high confidence
        assert result.confidence >= HybridClassifier.ML_THRESHOLD

        # Check metrics
        metrics = hybrid_classifier_no_ml.get_metrics()
        assert metrics["llm_used"] == 1
        assert metrics["ml_used"] == 0

    @pytest.mark.asyncio
    async def test_multiple_classifications_distribution(self, hybrid_classifier):
        """Test distribution across multiple classifications."""
        requests = [
            # High confidence heuristic matches
            ClassificationRequest(
                task_description="Fix the authentication bug error failure"
            ),
            ClassificationRequest(
                task_description="Fix the critical crash in login system"
            ),
            ClassificationRequest(
                task_description="Implement new user registration feature"
            ),
            # Lower confidence - might use ML/LLM
            ClassificationRequest(task_description="Update the user module"),
            ClassificationRequest(task_description="Modify authentication flow"),
        ]

        for request in requests:
            await hybrid_classifier.classify(request)

        metrics = hybrid_classifier.get_metrics()

        # Should have classified all 5 requests
        assert metrics["total_classifications"] == 5

        # Most should use heuristic (high confidence keywords)
        assert metrics["heuristic_used"] >= 3

        # Distribution percentages should sum to 100
        dist = hybrid_classifier.metrics.get_distribution()
        total_percent = (
            dist["heuristic_percent"] + dist["ml_percent"] + dist["llm_percent"]
        )
        assert abs(total_percent - 100.0) < 0.1


class TestClassificationMetrics:
    """Test metrics tracking."""

    def test_metrics_initialization(self):
        """Test that metrics start at zero."""
        metrics = ClassificationMetrics()

        assert metrics.total_classifications == 0
        assert metrics.heuristic_used == 0
        assert metrics.ml_used == 0
        assert metrics.llm_used == 0
        assert metrics.circuit_breaker_trips == 0
        assert metrics.timeouts == 0

    def test_record_classification(self):
        """Test recording classification events."""
        metrics = ClassificationMetrics()

        metrics.record_classification("heuristic", 5.0)
        metrics.record_classification("ml", 50.0)
        metrics.record_classification("llm", 800.0)

        assert metrics.total_classifications == 3
        assert metrics.heuristic_used == 1
        assert metrics.ml_used == 1
        assert metrics.llm_used == 1
        assert abs(metrics.get_average_latency_ms() - 285.0) < 0.1

    def test_get_distribution_empty(self):
        """Test distribution with no classifications."""
        metrics = ClassificationMetrics()
        dist = metrics.get_distribution()

        assert dist["heuristic_percent"] == 0.0
        assert dist["ml_percent"] == 0.0
        assert dist["llm_percent"] == 0.0

    def test_get_distribution(self):
        """Test distribution calculation."""
        metrics = ClassificationMetrics()

        # Record 85 heuristic, 14 ML, 1 LLM (target distribution)
        for _ in range(85):
            metrics.record_classification("heuristic", 5.0)
        for _ in range(14):
            metrics.record_classification("ml", 50.0)
        for _ in range(1):
            metrics.record_classification("llm", 800.0)

        dist = metrics.get_distribution()

        assert abs(dist["heuristic_percent"] - 85.0) < 0.1
        assert abs(dist["ml_percent"] - 14.0) < 0.1
        assert abs(dist["llm_percent"] - 1.0) < 0.1

    def test_get_summary(self):
        """Test comprehensive metrics summary."""
        metrics = ClassificationMetrics()
        metrics.record_classification("heuristic", 5.0)
        metrics.record_classification("ml", 50.0)

        summary = metrics.get_summary()

        assert summary["total_classifications"] == 2
        assert summary["heuristic_used"] == 1
        assert summary["ml_used"] == 1
        assert summary["heuristic_percent"] == 50.0
        assert summary["ml_percent"] == 50.0
        assert "average_latency_ms" in summary


class TestCircuitBreaker:
    """Test circuit breaker functionality."""

    def test_circuit_breaker_initialization(self):
        """Test circuit breaker starts closed."""
        cb = CircuitBreaker(failure_threshold=5, recovery_timeout=30.0)

        assert not cb.is_open
        assert cb.failure_count == 0
        assert cb.can_attempt()

    def test_circuit_breaker_opens_after_threshold(self):
        """Test circuit breaker opens after failure threshold."""
        cb = CircuitBreaker(failure_threshold=3, recovery_timeout=30.0)

        # Record failures
        cb.record_failure()
        assert cb.failure_count == 1
        assert not cb.is_open
        assert cb.can_attempt()

        cb.record_failure()
        assert cb.failure_count == 2
        assert not cb.is_open

        cb.record_failure()
        assert cb.failure_count == 3
        assert cb.is_open
        assert not cb.can_attempt()

    def test_circuit_breaker_recovers_after_timeout(self):
        """Test circuit breaker recovers after timeout."""
        cb = CircuitBreaker(failure_threshold=2, recovery_timeout=0.1)

        # Open circuit
        cb.record_failure()
        cb.record_failure()
        assert cb.is_open

        # Wait for recovery
        import time

        time.sleep(0.15)

        # Should be able to attempt again
        assert cb.can_attempt()
        assert not cb.is_open

    def test_circuit_breaker_resets_on_success(self):
        """Test circuit breaker resets failure count on success."""
        cb = CircuitBreaker(failure_threshold=3, recovery_timeout=30.0)

        # Record some failures
        cb.record_failure()
        cb.record_failure()
        assert cb.failure_count == 2

        # Success resets
        cb.record_success()
        assert cb.failure_count == 0
        assert not cb.is_open


class TestHybridClassifierTimeout:
    """Test timeout handling."""

    @pytest.mark.asyncio
    async def test_classification_timeout_handling(self):
        """Test that timeout returns heuristic fallback."""

        # Create a classifier with very short timeout for testing
        class SlowLLMClassifier(LLMClassifier):
            async def classify(self, request):
                # Simulate slow LLM call
                await asyncio.sleep(10.0)
                return await super().classify(request)

        hybrid = HybridClassifier(
            heuristic=HeuristicClassifier(),
            ml_classifier=None,  # No ML to force LLM path
            llm_classifier=SlowLLMClassifier(),
            enable_circuit_breaker=False,
        )

        # Override timeout to be very short for testing
        hybrid.CLASSIFICATION_TIMEOUT = 0.1

        request = ClassificationRequest(
            task_description="Do something unclear"
        )

        # Should timeout and fallback to heuristic
        result = await hybrid.classify(request)

        # Should use heuristic fallback
        assert result.classifier_used == "heuristic"
        assert "timeout" in result.reasoning.lower()

        # Metrics should show timeout
        metrics = hybrid.get_metrics()
        assert metrics["timeouts"] == 1


class TestHybridClassifierCircuitBreaker:
    """Test circuit breaker integration."""

    @pytest.mark.asyncio
    async def test_circuit_breaker_prevents_llm_calls(self):
        """Test that circuit breaker prevents LLM calls after failures."""

        # Create a failing LLM classifier
        class FailingLLMClassifier(LLMClassifier):
            async def classify(self, request):
                raise Exception("LLM service unavailable")

        hybrid = HybridClassifier(
            heuristic=HeuristicClassifier(),
            ml_classifier=None,
            llm_classifier=FailingLLMClassifier(),
            enable_circuit_breaker=True,
        )

        # Set low failure threshold for testing
        hybrid.circuit_breaker.failure_threshold = 2

        request = ClassificationRequest(
            task_description="Something ambiguous"
        )

        # First call - LLM fails, falls back to heuristic
        result1 = await hybrid.classify(request)
        assert result1.classifier_used == "heuristic"
        assert "failed" in result1.reasoning.lower()

        # Second call - LLM fails again, circuit opens
        result2 = await hybrid.classify(request)
        assert result2.classifier_used == "heuristic"

        # Third call - circuit is open, doesn't even try LLM
        result3 = await hybrid.classify(request)
        assert result3.classifier_used == "heuristic"
        assert "circuit breaker" in result3.reasoning.lower()

        # Verify circuit breaker is open
        assert hybrid.circuit_breaker.is_open
        assert not hybrid.circuit_breaker.can_attempt()

        # Metrics should show circuit breaker trip
        metrics = hybrid.get_metrics()
        assert metrics["circuit_breaker_trips"] >= 1

    @pytest.mark.asyncio
    async def test_manual_circuit_breaker_reset(self):
        """Test manual circuit breaker reset."""
        hybrid = HybridClassifier(enable_circuit_breaker=True)

        # Manually open circuit with proper state
        import time
        hybrid.circuit_breaker.is_open = True
        hybrid.circuit_breaker.failure_count = 5
        hybrid.circuit_breaker.last_failure_time = time.time()
        assert not hybrid.circuit_breaker.can_attempt()

        # Reset
        hybrid.reset_circuit_breaker()

        # Should be closed now
        assert not hybrid.circuit_breaker.is_open
        assert hybrid.circuit_breaker.failure_count == 0
        assert hybrid.circuit_breaker.can_attempt()


class TestHybridClassifierPerformance:
    """Test performance characteristics."""

    @pytest.mark.asyncio
    async def test_heuristic_path_latency(self, hybrid_classifier):
        """Test that heuristic path is fast (<10ms including overhead)."""
        request = ClassificationRequest(
            task_description="Fix the critical authentication bug error"
        )

        import time

        start = time.perf_counter()
        result = await hybrid_classifier.classify(request)
        latency_ms = (time.perf_counter() - start) * 1000

        # Should use heuristic (fast path)
        assert result.classifier_used == "heuristic"

        # Should be fast (target <5ms, allow <20ms with async overhead)
        assert latency_ms < 20, f"Heuristic path too slow: {latency_ms:.1f}ms"

    @pytest.mark.asyncio
    async def test_average_latency_tracking(self, hybrid_classifier):
        """Test that average latency is tracked correctly."""
        requests = [
            ClassificationRequest(
                task_description="Fix bug"
            ),  # High confidence heuristic
            ClassificationRequest(
                task_description="Implement feature"
            ),  # High confidence heuristic
            ClassificationRequest(
                task_description="Update module"
            ),  # Lower confidence
        ]

        for request in requests:
            await hybrid_classifier.classify(request)

        metrics = hybrid_classifier.get_metrics()

        # Should have average latency
        assert metrics["average_latency_ms"] > 0
        # Should be reasonable (mostly heuristic = fast)
        assert metrics["average_latency_ms"] < 100


class TestHybridClassifierMetricsOperations:
    """Test metrics operations."""

    @pytest.mark.asyncio
    async def test_metrics_reset(self, hybrid_classifier):
        """Test that metrics can be reset."""
        # Generate some classifications
        request = ClassificationRequest(task_description="Fix the bug")
        await hybrid_classifier.classify(request)

        metrics_before = hybrid_classifier.get_metrics()
        assert metrics_before["total_classifications"] == 1

        # Reset
        hybrid_classifier.reset_metrics()

        metrics_after = hybrid_classifier.get_metrics()
        assert metrics_after["total_classifications"] == 0
        assert metrics_after["heuristic_used"] == 0
        assert metrics_after["ml_used"] == 0
        assert metrics_after["llm_used"] == 0

    @pytest.mark.asyncio
    async def test_metrics_accumulate(self, hybrid_classifier):
        """Test that metrics accumulate over multiple calls."""
        for i in range(10):
            request = ClassificationRequest(
                task_description=f"Fix bug number {i}"
            )
            await hybrid_classifier.classify(request)

        metrics = hybrid_classifier.get_metrics()
        assert metrics["total_classifications"] == 10
        assert metrics["heuristic_used"] >= 5  # Most should use heuristic
