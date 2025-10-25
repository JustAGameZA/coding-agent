"""Hybrid classifier combining heuristic, ML, and LLM approaches."""

import asyncio
import logging
import time
from typing import Optional

from api.schemas.classification import ClassificationRequest, ClassificationResult
from domain.classifiers.heuristic import HeuristicClassifier
from domain.classifiers.llm_classifier import LLMClassifier
from domain.classifiers.ml_classifier import MLClassifier

logger = logging.getLogger(__name__)


class ClassificationMetrics:
    """Metrics for tracking classifier usage and performance."""

    def __init__(self):
        """Initialize metrics counters."""
        self.total_classifications = 0
        self.heuristic_used = 0
        self.ml_used = 0
        self.llm_used = 0
        self.total_latency_ms = 0.0
        self.circuit_breaker_trips = 0
        self.timeouts = 0

    def record_classification(
        self, classifier_used: str, latency_ms: float
    ) -> None:
        """
        Record a classification event.

        Args:
            classifier_used: Which classifier was used ('heuristic', 'ml', 'llm')
            latency_ms: Time taken in milliseconds
        """
        self.total_classifications += 1
        self.total_latency_ms += latency_ms

        if classifier_used == "heuristic":
            self.heuristic_used += 1
        elif classifier_used == "ml":
            self.ml_used += 1
        elif classifier_used == "llm":
            self.llm_used += 1

    def get_distribution(self) -> dict[str, float]:
        """
        Get the distribution of classifier usage.

        Returns:
            Dictionary with percentages for each classifier
        """
        if self.total_classifications == 0:
            return {
                "heuristic_percent": 0.0,
                "ml_percent": 0.0,
                "llm_percent": 0.0,
            }

        return {
            "heuristic_percent": (
                self.heuristic_used / self.total_classifications
            )
            * 100,
            "ml_percent": (self.ml_used / self.total_classifications) * 100,
            "llm_percent": (self.llm_used / self.total_classifications) * 100,
        }

    def get_average_latency_ms(self) -> float:
        """
        Get average classification latency.

        Returns:
            Average latency in milliseconds
        """
        if self.total_classifications == 0:
            return 0.0
        return self.total_latency_ms / self.total_classifications

    def get_summary(self) -> dict:
        """
        Get comprehensive metrics summary.

        Returns:
            Dictionary with all metrics
        """
        distribution = self.get_distribution()
        return {
            "total_classifications": self.total_classifications,
            "heuristic_used": self.heuristic_used,
            "ml_used": self.ml_used,
            "llm_used": self.llm_used,
            "heuristic_percent": distribution["heuristic_percent"],
            "ml_percent": distribution["ml_percent"],
            "llm_percent": distribution["llm_percent"],
            "average_latency_ms": self.get_average_latency_ms(),
            "circuit_breaker_trips": self.circuit_breaker_trips,
            "timeouts": self.timeouts,
        }


class CircuitBreaker:
    """Simple circuit breaker for LLM classifier failures."""

    def __init__(
        self,
        failure_threshold: int = 5,
        recovery_timeout: float = 30.0,
    ):
        """
        Initialize circuit breaker.

        Args:
            failure_threshold: Number of failures before opening circuit
            recovery_timeout: Seconds to wait before attempting recovery
        """
        self.failure_threshold = failure_threshold
        self.recovery_timeout = recovery_timeout
        self.failure_count = 0
        self.last_failure_time: Optional[float] = None
        self.is_open = False

    def record_success(self) -> None:
        """Record a successful call."""
        self.failure_count = 0
        self.is_open = False

    def record_failure(self) -> None:
        """Record a failed call and potentially open the circuit."""
        self.failure_count += 1
        self.last_failure_time = time.time()

        if self.failure_count >= self.failure_threshold:
            self.is_open = True
            logger.warning(
                f"Circuit breaker opened after {self.failure_count} failures"
            )

    def can_attempt(self) -> bool:
        """
        Check if we can attempt a call.

        Returns:
            True if circuit is closed or recovery timeout has passed
        """
        if not self.is_open:
            return True

        # Check if recovery timeout has passed
        if self.last_failure_time is None:
            return True

        time_since_failure = time.time() - self.last_failure_time
        if time_since_failure >= self.recovery_timeout:
            logger.info(
                "Circuit breaker attempting recovery after "
                f"{time_since_failure:.1f}s"
            )
            self.is_open = False
            self.failure_count = 0
            return True

        return False


class HybridClassifier:
    """
    Three-tier hybrid classification system.

    Phase 1: Heuristic (fast, 90% accuracy) - 85% of traffic
    Phase 2: ML (medium, 95% accuracy) - 14% of traffic
    Phase 3: LLM (slow, 98% accuracy) - 1% of traffic
    """

    # Confidence thresholds
    HEURISTIC_THRESHOLD = 0.85
    ML_THRESHOLD = 0.70

    # Timeout for entire classification (5 seconds max)
    CLASSIFICATION_TIMEOUT = 5.0

    def __init__(
        self,
        heuristic: Optional[HeuristicClassifier] = None,
        ml_classifier: Optional[MLClassifier] = None,
        llm_classifier: Optional[LLMClassifier] = None,
        enable_circuit_breaker: bool = True,
    ):
        """
        Initialize hybrid classifier.

        Args:
            heuristic: Heuristic classifier instance
            ml_classifier: ML classifier instance
            llm_classifier: LLM classifier instance
            enable_circuit_breaker: Whether to enable circuit breaker for LLM
        """
        self.heuristic = heuristic or HeuristicClassifier()
        self.ml_classifier = ml_classifier
        self.llm_classifier = llm_classifier or LLMClassifier()
        self.metrics = ClassificationMetrics()
        self.circuit_breaker = (
            CircuitBreaker() if enable_circuit_breaker else None
        )

        logger.info("Hybrid classifier initialized")
        logger.info(f"  - Heuristic threshold: {self.HEURISTIC_THRESHOLD}")
        logger.info(f"  - ML threshold: {self.ML_THRESHOLD}")
        logger.info(f"  - Classification timeout: {self.CLASSIFICATION_TIMEOUT}s")
        logger.info(f"  - Circuit breaker: {enable_circuit_breaker}")

    async def classify(
        self, request: ClassificationRequest
    ) -> ClassificationResult:
        """
        Execute hybrid classification strategy with timeout.

        Args:
            request: Classification request with task description

        Returns:
            ClassificationResult from the most appropriate classifier

        Raises:
            asyncio.TimeoutError: If classification exceeds timeout
        """
        try:
            # Apply overall timeout to entire classification process
            result = await asyncio.wait_for(
                self._classify_internal(request),
                timeout=self.CLASSIFICATION_TIMEOUT,
            )
            return result
        except asyncio.TimeoutError:
            logger.error(
                f"Classification timeout after {self.CLASSIFICATION_TIMEOUT}s"
            )
            self.metrics.timeouts += 1

            # Return heuristic result as fallback
            start_time = time.perf_counter()
            result = self.heuristic.classify(request.task_description)
            latency_ms = (time.perf_counter() - start_time) * 1000

            # Override reasoning to indicate timeout
            result.reasoning = (
                f"Classification timeout - using heuristic fallback: {result.reasoning}"
            )
            self.metrics.record_classification("heuristic", latency_ms)

            return result

    async def _classify_internal(
        self, request: ClassificationRequest
    ) -> ClassificationResult:
        """
        Internal classification logic with cascade.

        Args:
            request: Classification request

        Returns:
            ClassificationResult from appropriate classifier
        """
        overall_start = time.perf_counter()

        # Phase 1: Try heuristic classification (5ms)
        logger.info(
            f"Phase 1: Heuristic classification for: {request.task_description[:50]}..."
        )
        start_time = time.perf_counter()
        heuristic_result = self.heuristic.classify(request.task_description)
        heuristic_latency_ms = (time.perf_counter() - start_time) * 1000

        logger.info(
            f"Heuristic result: type={heuristic_result.task_type.value}, "
            f"confidence={heuristic_result.confidence:.2f}, "
            f"latency={heuristic_latency_ms:.1f}ms"
        )

        if heuristic_result.confidence >= self.HEURISTIC_THRESHOLD:
            logger.info(
                f"Heuristic confidence {heuristic_result.confidence:.2f} >= "
                f"{self.HEURISTIC_THRESHOLD}, using heuristic result"
            )
            self.metrics.record_classification("heuristic", heuristic_latency_ms)
            return heuristic_result

        # Phase 2: Try ML classification (50ms)
        if self.ml_classifier is not None:
            logger.info(
                f"Phase 2: ML classification (heuristic confidence "
                f"{heuristic_result.confidence:.2f} < {self.HEURISTIC_THRESHOLD})"
            )
            try:
                start_time = time.perf_counter()
                ml_result = await self.ml_classifier.classify(request)
                ml_latency_ms = (time.perf_counter() - start_time) * 1000

                logger.info(
                    f"ML result: type={ml_result.task_type.value}, "
                    f"confidence={ml_result.confidence:.2f}, "
                    f"latency={ml_latency_ms:.1f}ms"
                )

                if ml_result.confidence >= self.ML_THRESHOLD:
                    logger.info(
                        f"ML confidence {ml_result.confidence:.2f} >= "
                        f"{self.ML_THRESHOLD}, using ML result"
                    )
                    total_latency_ms = (
                        time.perf_counter() - overall_start
                    ) * 1000
                    self.metrics.record_classification("ml", total_latency_ms)
                    return ml_result
            except Exception as e:
                logger.warning(f"ML classification failed: {e}, continuing to LLM")
        else:
            logger.info("ML classifier not available, skipping to LLM")

        # Phase 3: LLM classification (800ms) with circuit breaker
        logger.info(
            f"Phase 3: LLM classification (ML confidence < {self.ML_THRESHOLD})"
        )

        # Check circuit breaker
        if self.circuit_breaker and not self.circuit_breaker.can_attempt():
            logger.warning(
                "LLM circuit breaker is open, using ML or heuristic fallback"
            )
            self.metrics.circuit_breaker_trips += 1

            # Use ML result if available, otherwise heuristic
            fallback_result = (
                ml_result if self.ml_classifier else heuristic_result
            )
            fallback_result.reasoning = (
                f"Circuit breaker open - using fallback: {fallback_result.reasoning}"
            )

            total_latency_ms = (time.perf_counter() - overall_start) * 1000
            classifier_used = "ml" if self.ml_classifier else "heuristic"
            self.metrics.record_classification(classifier_used, total_latency_ms)

            return fallback_result

        # Attempt LLM classification
        try:
            start_time = time.perf_counter()
            llm_result = await self.llm_classifier.classify(request)
            llm_latency_ms = (time.perf_counter() - start_time) * 1000

            logger.info(
                f"LLM result: type={llm_result.task_type.value}, "
                f"confidence={llm_result.confidence:.2f}, "
                f"latency={llm_latency_ms:.1f}ms"
            )

            # Record success with circuit breaker
            if self.circuit_breaker:
                self.circuit_breaker.record_success()

            total_latency_ms = (time.perf_counter() - overall_start) * 1000
            self.metrics.record_classification("llm", total_latency_ms)

            return llm_result

        except Exception as e:
            logger.error(f"LLM classification failed: {e}")

            # Record failure with circuit breaker
            if self.circuit_breaker:
                self.circuit_breaker.record_failure()

            # Fallback to ML or heuristic
            fallback_result = (
                ml_result if self.ml_classifier else heuristic_result
            )
            fallback_result.reasoning = (
                f"LLM failed - using fallback: {fallback_result.reasoning}"
            )

            total_latency_ms = (time.perf_counter() - overall_start) * 1000
            classifier_used = "ml" if self.ml_classifier else "heuristic"
            self.metrics.record_classification(classifier_used, total_latency_ms)

            return fallback_result

    def get_metrics(self) -> dict:
        """
        Get classification metrics.

        Returns:
            Dictionary with metrics summary
        """
        return self.metrics.get_summary()

    def reset_metrics(self) -> None:
        """Reset all metrics counters."""
        self.metrics = ClassificationMetrics()
        logger.info("Metrics reset")

    def reset_circuit_breaker(self) -> None:
        """Manually reset circuit breaker."""
        if self.circuit_breaker:
            self.circuit_breaker.is_open = False
            self.circuit_breaker.failure_count = 0
            logger.info("Circuit breaker manually reset")
