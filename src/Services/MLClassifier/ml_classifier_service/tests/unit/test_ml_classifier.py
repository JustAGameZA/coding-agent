"""Unit tests for ML classifier."""

import time

import numpy as np
import pytest
from domain.classifiers.ml_classifier import MLClassifier
from domain.models.task_type import TaskComplexity, TaskType
from api.schemas.classification import ClassificationRequest


@pytest.fixture
def ml_classifier():
    """Create an ML classifier instance with loaded model."""
    return MLClassifier.load_from_disk()


class TestMLClassifier:
    """Test suite for ML classifier."""

    def test_classifier_initialization(self, ml_classifier):
        """Test that classifier initializes correctly."""
        assert ml_classifier.model is not None
        assert ml_classifier.feature_extractor is not None
        assert ml_classifier.metadata is not None
        assert ml_classifier.get_model_version() is not None
        assert ml_classifier.get_model_accuracy() > 0.0

    def test_model_version_tracking(self, ml_classifier):
        """Test model version tracking."""
        version = ml_classifier.get_model_version()
        assert isinstance(version, str)
        assert len(version) > 0
        assert "v" in version.lower() or "dummy" in version.lower()

    def test_model_accuracy_tracking(self, ml_classifier):
        """Test model accuracy tracking."""
        accuracy = ml_classifier.get_model_accuracy()
        assert isinstance(accuracy, float)
        assert 0.0 <= accuracy <= 1.0

    @pytest.mark.asyncio
    async def test_classify_bug_fix(self, ml_classifier):
        """Test classification of a bug fix task."""
        request = ClassificationRequest(
            task_description="Fix the login bug where users can't authenticate"
        )
        result = await ml_classifier.classify(request)

        assert result.task_type == TaskType.BUG_FIX
        assert 0.0 <= result.confidence <= 1.0
        assert result.classifier_used == "ml"
        assert result.reasoning is not None
        assert len(result.reasoning) > 0

    @pytest.mark.asyncio
    async def test_classify_feature(self, ml_classifier):
        """Test classification of a feature task."""
        request = ClassificationRequest(
            task_description="Implement a new user registration feature with email verification"
        )
        result = await ml_classifier.classify(request)

        assert result.task_type == TaskType.FEATURE
        assert 0.0 <= result.confidence <= 1.0
        assert result.classifier_used == "ml"

    @pytest.mark.asyncio
    async def test_classify_refactor(self, ml_classifier):
        """Test classification of a refactor task."""
        request = ClassificationRequest(
            task_description="Refactor the authentication module to improve code quality"
        )
        result = await ml_classifier.classify(request)

        assert result.task_type == TaskType.REFACTOR
        assert 0.0 <= result.confidence <= 1.0
        assert result.classifier_used == "ml"

    @pytest.mark.asyncio
    async def test_classify_test(self, ml_classifier):
        """Test classification of a test task."""
        request = ClassificationRequest(
            task_description="Write unit tests for the user service with 90% coverage"
        )
        result = await ml_classifier.classify(request)

        assert result.task_type == TaskType.TEST
        assert 0.0 <= result.confidence <= 1.0
        assert result.classifier_used == "ml"

    @pytest.mark.asyncio
    async def test_classify_documentation(self, ml_classifier):
        """Test classification of a documentation task."""
        request = ClassificationRequest(
            task_description="Update the README with installation instructions and examples"
        )
        result = await ml_classifier.classify(request)

        assert result.task_type == TaskType.DOCUMENTATION
        assert 0.0 <= result.confidence <= 1.0
        assert result.classifier_used == "ml"

    @pytest.mark.asyncio
    async def test_classify_deployment(self, ml_classifier):
        """Test classification of a deployment task."""
        request = ClassificationRequest(
            task_description="Deploy the application to Kubernetes cluster with Helm charts"
        )
        result = await ml_classifier.classify(request)

        assert result.task_type == TaskType.DEPLOYMENT
        assert 0.0 <= result.confidence <= 1.0
        assert result.classifier_used == "ml"

    @pytest.mark.asyncio
    async def test_classify_simple_complexity(self, ml_classifier):
        """Test classification of simple task complexity."""
        request = ClassificationRequest(
            task_description="Fix a small typo"
        )
        result = await ml_classifier.classify(request)

        assert result.complexity == TaskComplexity.SIMPLE
        assert result.suggested_strategy == "SingleShot"
        assert result.estimated_tokens == 2000

    @pytest.mark.asyncio
    async def test_classify_medium_complexity(self, ml_classifier):
        """Test classification of medium task complexity."""
        request = ClassificationRequest(
            task_description="Implement user authentication with JWT tokens, password hashing, and session management"
        )
        result = await ml_classifier.classify(request)

        assert result.complexity in [TaskComplexity.SIMPLE, TaskComplexity.MEDIUM]
        assert result.suggested_strategy in ["SingleShot", "Iterative"]

    @pytest.mark.asyncio
    async def test_classify_complex_complexity(self, ml_classifier):
        """Test classification of complex task complexity."""
        request = ClassificationRequest(
            task_description=(
                "Implement a complex microservices architecture with API gateway, "
                "multiple backend services, message queues, caching layer, "
                "database sharding, and comprehensive monitoring. "
                "This is a major refactor that will touch the entire system."
            )
        )
        result = await ml_classifier.classify(request)

        assert result.complexity == TaskComplexity.COMPLEX
        assert result.suggested_strategy == "MultiAgent"
        assert result.estimated_tokens == 20000

    @pytest.mark.asyncio
    async def test_confidence_threshold(self, ml_classifier):
        """Test that ML confidence threshold is properly set."""
        assert ml_classifier.ML_CONFIDENCE_THRESHOLD == 0.70

    @pytest.mark.asyncio
    async def test_confidence_score_range(self, ml_classifier):
        """Test that confidence scores are within valid range."""
        descriptions = [
            "Fix the login bug",
            "Add new feature",
            "Refactor code",
            "Write tests",
            "Update documentation",
            "Deploy to production",
        ]

        for description in descriptions:
            request = ClassificationRequest(task_description=description)
            result = await ml_classifier.classify(request)
            assert 0.0 <= result.confidence <= 1.0

    @pytest.mark.asyncio
    async def test_reasoning_provided(self, ml_classifier):
        """Test that reasoning is always provided."""
        request = ClassificationRequest(
            task_description="Fix a bug in the authentication system"
        )
        result = await ml_classifier.classify(request)

        assert result.reasoning is not None
        assert len(result.reasoning) > 0
        assert "ML model" in result.reasoning or "predicted" in result.reasoning.lower()

    @pytest.mark.asyncio
    async def test_estimated_tokens_positive(self, ml_classifier):
        """Test that estimated tokens are always positive."""
        request = ClassificationRequest(
            task_description="Fix a bug in the authentication system"
        )
        result = await ml_classifier.classify(request)

        assert result.estimated_tokens > 0

    @pytest.mark.asyncio
    async def test_with_context(self, ml_classifier):
        """Test classification with additional context."""
        request = ClassificationRequest(
            task_description="Fix authentication issue",
            context={"repository": "backend-api"},
            files_changed=["src/auth/login.py"],
        )
        result = await ml_classifier.classify(request)

        assert result.task_type in TaskType
        assert result.confidence > 0.0

    @pytest.mark.asyncio
    async def test_multiple_classifications_consistency(self, ml_classifier):
        """Test that same input gives consistent results."""
        request = ClassificationRequest(
            task_description="Fix the login bug where users can't authenticate"
        )

        result1 = await ml_classifier.classify(request)
        result2 = await ml_classifier.classify(request)

        assert result1.task_type == result2.task_type
        assert result1.confidence == result2.confidence
        assert result1.complexity == result2.complexity


class TestMLClassifierPerformance:
    """Performance tests for ML classifier."""

    @pytest.mark.asyncio
    async def test_latency_under_50ms(self, ml_classifier):
        """Test that classification latency is under 50ms (target)."""
        request = ClassificationRequest(
            task_description="Fix the login bug where users can't authenticate"
        )

        # Warm-up run
        await ml_classifier.classify(request)

        # Measure latency over multiple runs
        latencies = []
        num_runs = 10

        for _ in range(num_runs):
            start_time = time.perf_counter()
            await ml_classifier.classify(request)
            end_time = time.perf_counter()
            latency_ms = (end_time - start_time) * 1000
            latencies.append(latency_ms)

        avg_latency = sum(latencies) / len(latencies)
        p95_latency = sorted(latencies)[int(len(latencies) * 0.95)]

        print(f"\nLatency stats:")
        print(f"  Average: {avg_latency:.2f}ms")
        print(f"  P95: {p95_latency:.2f}ms")
        print(f"  Min: {min(latencies):.2f}ms")
        print(f"  Max: {max(latencies):.2f}ms")

        # Target is < 50ms average
        assert avg_latency < 50, f"Average latency {avg_latency:.2f}ms exceeds 50ms target"

    @pytest.mark.asyncio
    async def test_batch_classification_performance(self, ml_classifier):
        """Test classification performance with multiple tasks."""
        descriptions = [
            "Fix the login bug",
            "Add new feature for user registration",
            "Refactor authentication module",
            "Write unit tests for API",
            "Update documentation",
            "Deploy to Kubernetes",
            "Fix error in payment processing",
            "Implement OAuth2 support",
            "Optimize database queries",
            "Add integration tests",
        ]

        start_time = time.perf_counter()

        for description in descriptions:
            request = ClassificationRequest(task_description=description)
            await ml_classifier.classify(request)

        end_time = time.perf_counter()
        total_time = (end_time - start_time) * 1000
        avg_time_per_task = total_time / len(descriptions)

        print(f"\nBatch classification stats:")
        print(f"  Total time: {total_time:.2f}ms")
        print(f"  Average per task: {avg_time_per_task:.2f}ms")
        print(f"  Tasks per second: {1000 / avg_time_per_task:.2f}")

        # Should handle at least 20 tasks per second (50ms each)
        assert avg_time_per_task < 50


class TestMLClassifierFeatureExtraction:
    """Test feature extraction functionality."""

    def test_feature_extraction(self, ml_classifier):
        """Test that feature extraction works correctly."""
        description = "Fix the login bug where users can't authenticate"
        features = ml_classifier.feature_extractor.extract_features(description)

        assert features is not None
        assert isinstance(features, np.ndarray)
        assert len(features) > 0
        assert features.dtype == np.float32

    def test_feature_count(self, ml_classifier):
        """Test that feature count matches expected value."""
        feature_count = ml_classifier.feature_extractor.feature_count
        assert feature_count > 0
        assert feature_count == ml_classifier.metadata.feature_count

    def test_feature_names(self, ml_classifier):
        """Test that feature names are available."""
        feature_names = ml_classifier.feature_extractor.get_feature_names()
        assert len(feature_names) > 0
        assert len(feature_names) == ml_classifier.feature_extractor.feature_count

    def test_tfidf_features(self, ml_classifier):
        """Test that TF-IDF features are extracted."""
        description = "Fix bug error crash issue problem"
        features = ml_classifier.feature_extractor.extract_features(description)

        # TF-IDF features should be in the feature vector
        assert len(features) > 100  # At least the TF-IDF features

    def test_keyword_features(self, ml_classifier):
        """Test that keyword features are extracted."""
        description = "Fix the bug and add new feature"
        features = ml_classifier.feature_extractor.extract_features(description)

        # Should have keyword presence and count features
        feature_names = ml_classifier.feature_extractor.get_feature_names()
        keyword_features = [
            name for name in feature_names if "keyword" in name.lower()
        ]
        assert len(keyword_features) > 0

    def test_code_pattern_detection(self, ml_classifier):
        """Test that code patterns are detected."""
        description = """
        Fix the bug in the `authenticate()` function:
        ```python
        def authenticate(user):
            return user.is_valid()
        ```
        """
        features = ml_classifier.feature_extractor.extract_features(description)

        # Should detect code patterns
        assert features is not None
        assert len(features) > 0
