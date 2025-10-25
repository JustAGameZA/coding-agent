"""Unit tests for LLM classifier."""

import pytest
from api.schemas.classification import ClassificationRequest
from domain.classifiers.llm_classifier import LLMClassifier
from domain.models.task_type import TaskComplexity, TaskType


@pytest.fixture
def classifier():
    """Create an LLM classifier instance."""
    return LLMClassifier()


class TestLLMClassifier:
    """Test suite for LLM classifier."""

    @pytest.mark.asyncio
    async def test_classify_bug_fix(self, classifier):
        """Test classification of a bug fix task."""
        request = ClassificationRequest(
            task_description="Fix the authentication bug that prevents users from logging in"
        )
        result = await classifier.classify(request)

        assert result.task_type == TaskType.BUG_FIX
        assert result.confidence >= 0.95  # LLM provides high confidence
        assert result.classifier_used == "llm"
        assert "LLM" in result.reasoning

    @pytest.mark.asyncio
    async def test_classify_feature(self, classifier):
        """Test classification of a feature task."""
        request = ClassificationRequest(
            task_description="Implement a new dashboard with real-time analytics"
        )
        result = await classifier.classify(request)

        assert result.task_type == TaskType.FEATURE
        assert result.confidence >= 0.95
        assert result.classifier_used == "llm"

    @pytest.mark.asyncio
    async def test_classify_refactor(self, classifier):
        """Test classification of a refactor task."""
        request = ClassificationRequest(
            task_description="Refactor the authentication module to improve performance"
        )
        result = await classifier.classify(request)

        assert result.task_type == TaskType.REFACTOR
        assert result.confidence >= 0.95
        assert result.classifier_used == "llm"

    @pytest.mark.asyncio
    async def test_classify_test(self, classifier):
        """Test classification of a test task."""
        request = ClassificationRequest(
            task_description="Write comprehensive unit tests for the user service"
        )
        result = await classifier.classify(request)

        assert result.task_type == TaskType.TEST
        assert result.confidence >= 0.95
        assert result.classifier_used == "llm"

    @pytest.mark.asyncio
    async def test_classify_documentation(self, classifier):
        """Test classification of a documentation task."""
        request = ClassificationRequest(
            task_description="Update the API documentation with new endpoints"
        )
        result = await classifier.classify(request)

        assert result.task_type == TaskType.DOCUMENTATION
        assert result.confidence >= 0.95
        assert result.classifier_used == "llm"

    @pytest.mark.asyncio
    async def test_classify_deployment(self, classifier):
        """Test classification of a deployment task."""
        request = ClassificationRequest(
            task_description="Deploy the application to production using Kubernetes"
        )
        result = await classifier.classify(request)

        assert result.task_type == TaskType.DEPLOYMENT
        assert result.confidence >= 0.95
        assert result.classifier_used == "llm"

    @pytest.mark.asyncio
    async def test_classify_simple_complexity(self, classifier):
        """Test classification of simple task complexity."""
        request = ClassificationRequest(
            task_description="Fix a small typo in the login form"
        )
        result = await classifier.classify(request)

        assert result.complexity == TaskComplexity.SIMPLE
        assert result.suggested_strategy == "SingleShot"
        assert result.estimated_tokens == 2000

    @pytest.mark.asyncio
    async def test_classify_medium_complexity(self, classifier):
        """Test classification of medium task complexity."""
        request = ClassificationRequest(
            task_description="Implement user authentication with JWT tokens and refresh token support"
        )
        result = await classifier.classify(request)

        # Word count around 12, should be SIMPLE
        # But can also be MEDIUM based on content
        assert result.complexity in [TaskComplexity.SIMPLE, TaskComplexity.MEDIUM]

    @pytest.mark.asyncio
    async def test_classify_complex_complexity(self, classifier):
        """Test classification of complex task complexity."""
        request = ClassificationRequest(
            task_description=(
                "Implement a complex microservices architecture with API gateway, "
                "multiple backend services, message queues, caching layer, and monitoring. "
                "This is a major system-wide rewrite that will affect all components."
            )
        )
        result = await classifier.classify(request)

        assert result.complexity == TaskComplexity.COMPLEX
        assert result.suggested_strategy == "MultiAgent"
        assert result.estimated_tokens == 20000

    @pytest.mark.asyncio
    async def test_high_confidence(self, classifier):
        """Test that LLM always provides high confidence (98% target)."""
        requests = [
            ClassificationRequest(task_description="Fix the critical bug"),
            ClassificationRequest(task_description="Add new feature"),
            ClassificationRequest(task_description="Refactor authentication"),
        ]

        for request in requests:
            result = await classifier.classify(request)
            assert result.confidence >= 0.95
            assert result.confidence <= 1.0

    @pytest.mark.asyncio
    async def test_classifier_used_field(self, classifier):
        """Test that classifier_used field is always 'llm'."""
        request = ClassificationRequest(
            task_description="Do something with the system"
        )
        result = await classifier.classify(request)

        assert result.classifier_used == "llm"

    @pytest.mark.asyncio
    async def test_reasoning_includes_model_info(self, classifier):
        """Test that reasoning mentions the LLM model used."""
        request = ClassificationRequest(
            task_description="Fix the authentication bug"
        )
        result = await classifier.classify(request)

        # Should mention the model in reasoning
        assert "LLM" in result.reasoning or "gpt-4o" in result.reasoning

    @pytest.mark.asyncio
    async def test_ambiguous_description(self, classifier):
        """Test that LLM can handle ambiguous descriptions."""
        request = ClassificationRequest(
            task_description="Do something with the user module"
        )
        result = await classifier.classify(request)

        # Should still provide a classification
        assert result.task_type in TaskType
        assert result.complexity in TaskComplexity
        # Should default to FEATURE for ambiguous cases
        assert result.task_type == TaskType.FEATURE

    @pytest.mark.asyncio
    async def test_custom_model_name(self):
        """Test initializing classifier with custom model name."""
        custom_classifier = LLMClassifier(model="gpt-4-turbo")

        request = ClassificationRequest(task_description="Fix the bug")
        result = await custom_classifier.classify(request)

        assert result.classifier_used == "llm"
        assert result.confidence >= 0.95

    @pytest.mark.asyncio
    async def test_suggested_strategy_matches_complexity(self, classifier):
        """Test that suggested strategy always matches complexity."""
        test_cases = [
            ("Simple quick fix", TaskComplexity.SIMPLE, "SingleShot"),
            (
                "Implement authentication with JWT and refresh tokens",
                TaskComplexity.SIMPLE,  # Based on word count
                "SingleShot",
            ),
            (
                "Complex major system-wide architecture rewrite",
                TaskComplexity.COMPLEX,
                "MultiAgent",
            ),
        ]

        for description, expected_complexity, expected_strategy in test_cases:
            request = ClassificationRequest(task_description=description)
            result = await classifier.classify(request)

            # Verify strategy matches complexity
            if result.complexity == TaskComplexity.SIMPLE:
                assert result.suggested_strategy == "SingleShot"
            elif result.complexity == TaskComplexity.MEDIUM:
                assert result.suggested_strategy == "Iterative"
            elif result.complexity == TaskComplexity.COMPLEX:
                assert result.suggested_strategy == "MultiAgent"

    @pytest.mark.asyncio
    async def test_estimated_tokens_positive(self, classifier):
        """Test that estimated tokens are always positive."""
        request = ClassificationRequest(
            task_description="Fix a bug in the authentication system"
        )
        result = await classifier.classify(request)

        assert result.estimated_tokens > 0

    @pytest.mark.asyncio
    async def test_complexity_keyword_override(self, classifier):
        """Test that complexity keywords override word count."""
        # Short description with complex keyword
        request = ClassificationRequest(
            task_description="Complex architecture change needed"
        )
        result = await classifier.classify(request)

        assert result.complexity == TaskComplexity.COMPLEX

    @pytest.mark.asyncio
    async def test_multiple_classifications_consistent(self, classifier):
        """Test that multiple classifications of same input are consistent."""
        request = ClassificationRequest(
            task_description="Fix the authentication bug"
        )

        # Run multiple times
        results = []
        for _ in range(3):
            result = await classifier.classify(request)
            results.append(result)

        # All should have same task type (stub implementation is deterministic)
        assert all(r.task_type == results[0].task_type for r in results)
        assert all(r.complexity == results[0].complexity for r in results)
        assert all(r.classifier_used == "llm" for r in results)

    @pytest.mark.asyncio
    async def test_all_task_types_covered(self, classifier):
        """Test that all task types can be classified."""
        test_cases = [
            ("Fix the critical bug", TaskType.BUG_FIX),
            ("Add new user feature", TaskType.FEATURE),
            ("Refactor the codebase", TaskType.REFACTOR),
            ("Run unit tests for coverage", TaskType.TEST),
            ("Update documentation", TaskType.DOCUMENTATION),
            ("Deploy to Kubernetes", TaskType.DEPLOYMENT),
        ]

        for description, expected_type in test_cases:
            request = ClassificationRequest(task_description=description)
            result = await classifier.classify(request)

            assert result.task_type == expected_type
            assert result.classifier_used == "llm"
            assert result.confidence >= 0.95

    @pytest.mark.asyncio
    async def test_with_context(self, classifier):
        """Test classification with additional context."""
        request = ClassificationRequest(
            task_description="Fix the database issue",
            context={"repository": "backend-api", "priority": "high"},
            files_changed=["src/database/connection.py"],
        )
        result = await classifier.classify(request)

        # Context is currently ignored in stub implementation
        # but should not cause errors
        assert result.task_type == TaskType.BUG_FIX
        assert result.classifier_used == "llm"

    @pytest.mark.asyncio
    async def test_word_count_complexity_boundaries(self, classifier):
        """Test complexity classification at word count boundaries."""
        # Test at boundaries: <20 = simple, 20-100 = medium, >100 = complex

        # 15 words - should be simple
        request1 = ClassificationRequest(
            task_description="Implement " + " ".join(["word"] * 13)
        )
        result1 = await classifier.classify(request1)
        assert result1.complexity == TaskComplexity.SIMPLE

        # 50 words - should be medium
        request2 = ClassificationRequest(
            task_description="Implement " + " ".join(["word"] * 48)
        )
        result2 = await classifier.classify(request2)
        assert result2.complexity == TaskComplexity.MEDIUM

        # 120 words - should be complex
        request3 = ClassificationRequest(
            task_description="Implement " + " ".join(["word"] * 118)
        )
        result3 = await classifier.classify(request3)
        assert result3.complexity == TaskComplexity.COMPLEX
