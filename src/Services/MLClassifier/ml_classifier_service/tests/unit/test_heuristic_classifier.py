"""Unit tests for heuristic classifier."""

import pytest
from domain.classifiers.heuristic import HeuristicClassifier
from domain.models.task_type import TaskComplexity, TaskType


@pytest.fixture
def classifier():
    """Create a heuristic classifier instance."""
    return HeuristicClassifier()


class TestHeuristicClassifier:
    """Test suite for heuristic classifier."""

    def test_classify_bug_fix(self, classifier):
        """Test classification of a bug fix task."""
        description = "Fix the login bug where users can't authenticate"
        result = classifier.classify(description)

        assert result.task_type == TaskType.BUG_FIX
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"
        assert "bug" in result.reasoning.lower() or "fix" in result.reasoning.lower()

    def test_classify_feature(self, classifier):
        """Test classification of a feature task."""
        description = (
            "Implement a new user registration feature with email verification"
        )
        result = classifier.classify(description)

        assert result.task_type == TaskType.FEATURE
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"

    def test_classify_refactor(self, classifier):
        """Test classification of a refactor task."""
        description = "Refactor the authentication module to improve code quality"
        result = classifier.classify(description)

        assert result.task_type == TaskType.REFACTOR
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"

    def test_classify_test(self, classifier):
        """Test classification of a test task."""
        description = "Write unit tests for the user service with 90% coverage"
        result = classifier.classify(description)

        assert result.task_type == TaskType.TEST
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"

    def test_classify_documentation(self, classifier):
        """Test classification of a documentation task."""
        description = "Update the README with installation instructions and examples"
        result = classifier.classify(description)

        assert result.task_type == TaskType.DOCUMENTATION
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"

    def test_classify_deployment(self, classifier):
        """Test classification of a deployment task."""
        description = "Deploy the application to Kubernetes cluster with Helm charts"
        result = classifier.classify(description)

        assert result.task_type == TaskType.DEPLOYMENT
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"

    def test_classify_simple_complexity(self, classifier):
        """Test classification of simple task complexity."""
        description = "Fix a small typo in the login form"
        result = classifier.classify(description)

        assert result.complexity == TaskComplexity.SIMPLE
        assert result.suggested_strategy == "SingleShot"
        assert result.estimated_tokens == 2000

    def test_classify_medium_complexity(self, classifier):
        """Test classification of medium task complexity."""
        description = "Implement user authentication with JWT tokens, password hashing, and session management"
        result = classifier.classify(description)

        assert result.complexity in [TaskComplexity.SIMPLE, TaskComplexity.MEDIUM]
        assert result.suggested_strategy in ["SingleShot", "Iterative"]

    def test_classify_medium_complexity_by_word_count(self, classifier):
        """Test medium complexity classification based on word count (20-100 words)."""
        # Create a description with exactly 50 words (between 20 and 100)
        description = "Implement a feature to " + " ".join(["word"] * 47)
        result = classifier.classify(description)

        assert result.complexity == TaskComplexity.MEDIUM
        assert result.suggested_strategy == "Iterative"
        assert result.estimated_tokens == 6000

    def test_classify_simple_complexity_by_word_count(self, classifier):
        """Test simple complexity classification based on word count (<20 words)."""
        # Create a description with exactly 15 words (less than 20)
        description = "Fix a " + " ".join(["word"] * 13)
        result = classifier.classify(description)

        assert result.complexity == TaskComplexity.SIMPLE
        assert result.suggested_strategy == "SingleShot"
        assert result.estimated_tokens == 2000

    def test_classify_complex_complexity_by_word_count(self, classifier):
        """Test complex complexity classification based on word count (>100 words)."""
        # Create a description with exactly 120 words (more than 100)
        description = "Implement a " + " ".join(["word"] * 118)
        result = classifier.classify(description)

        assert result.complexity == TaskComplexity.COMPLEX
        assert result.suggested_strategy == "MultiAgent"
        assert result.estimated_tokens == 20000

    def test_classify_complex_complexity(self, classifier):
        """Test classification of complex task complexity."""
        description = (
            "Implement a complex microservices architecture with API gateway, "
            "multiple backend services, message queues, caching layer, "
            "database sharding, and comprehensive monitoring. "
            "This is a major refactor that will touch the entire system."
        )
        result = classifier.classify(description)

        assert result.complexity == TaskComplexity.COMPLEX
        assert result.suggested_strategy == "MultiAgent"
        assert result.estimated_tokens == 20000

    def test_classify_no_matches(self, classifier):
        """Test classification when no keywords match."""
        description = "Do something unspecified"
        result = classifier.classify(description)

        # Should default to FEATURE with low confidence
        assert result.task_type == TaskType.FEATURE
        assert result.confidence < 0.5
        assert result.classifier_used == "heuristic"

    def test_classify_multiple_matches(self, classifier):
        """Test classification with multiple keyword matches."""
        description = "Fix the bug in the new feature implementation"
        result = classifier.classify(description)

        # Should pick the type with most matches
        assert result.task_type in [TaskType.BUG_FIX, TaskType.FEATURE]
        assert result.confidence > 0.0

    def test_confidence_score_range(self, classifier):
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
            result = classifier.classify(description)
            assert 0.0 <= result.confidence <= 1.0

    def test_estimated_tokens_positive(self, classifier):
        """Test that estimated tokens are always positive."""
        description = "Fix a bug in the authentication system"
        result = classifier.classify(description)

        assert result.estimated_tokens > 0

    def test_case_insensitive_matching(self, classifier):
        """Test that keyword matching is case-insensitive."""
        descriptions = [
            "FIX THE BUG",
            "Fix The Bug",
            "fix the bug",
            "fIx ThE bUg",
        ]

        for description in descriptions:
            result = classifier.classify(description)
            assert result.task_type == TaskType.BUG_FIX
            assert result.confidence > 0.5

    def test_high_confidence_single_type(self, classifier):
        """Test high confidence when all matches are for single type."""
        description = "Fix the critical bug error failure in broken authentication"
        result = classifier.classify(description)

        assert result.task_type == TaskType.BUG_FIX
        # Should have boosted confidence due to unique matches
        assert result.confidence >= 0.85

    def test_reasoning_includes_matched_keywords(self, classifier):
        """Test that reasoning includes matched keyword patterns."""
        description = "Fix the login bug that causes crashes"
        result = classifier.classify(description)

        assert result.task_type == TaskType.BUG_FIX
        # Reasoning should mention bug_fix and include some matched patterns
        assert "bug_fix" in result.reasoning.lower()
        assert len(result.reasoning) > 0

    def test_complexity_simple_keyword_override(self, classifier):
        """Test that simple complexity keywords override word count."""
        # Long description but has 'simple' keyword
        description = "This is a simple fix " + " ".join(["word"] * 95)
        result = classifier.classify(description)

        assert result.complexity == TaskComplexity.SIMPLE

    def test_complexity_complex_keyword_override(self, classifier):
        """Test that complex complexity keywords override word count."""
        # Short description but has 'complex' keyword
        description = "Complex architecture change needed"
        result = classifier.classify(description)

        assert result.complexity == TaskComplexity.COMPLEX

    def test_all_task_types_covered(self, classifier):
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
            result = classifier.classify(description)
            assert result.task_type == expected_type
            assert result.classifier_used == "heuristic"

    def test_performance_fast_classification(self, classifier):
        """Test that classification is fast (<5ms target)."""
        import time

        description = "Fix the authentication bug in the login system"

        # Warm up
        classifier.classify(description)

        # Measure average over 100 iterations
        iterations = 100
        start = time.perf_counter()
        for _ in range(iterations):
            classifier.classify(description)
        end = time.perf_counter()

        avg_time_ms = ((end - start) / iterations) * 1000
        # Target is <5ms, we assert <10ms to be safe with CI variability
        assert avg_time_ms < 10, f"Average classification time {avg_time_ms:.2f}ms exceeds 10ms"

    def test_multiple_keywords_same_type(self, classifier):
        """Test classification with multiple keywords from same type."""
        description = "Fix the bug and resolve the error that causes crashes"
        result = classifier.classify(description)

        assert result.task_type == TaskType.BUG_FIX
        # Should have high confidence due to multiple matches
        assert result.confidence > 0.7

    def test_suggested_strategy_matches_complexity(self, classifier):
        """Test that suggested strategy always matches complexity level."""
        test_cases = [
            (TaskComplexity.SIMPLE, "SingleShot", "Simple quick fix with typo"),
            (TaskComplexity.MEDIUM, "Iterative", " ".join(["word"] * 50)),
            (TaskComplexity.COMPLEX, "MultiAgent", "Complex major architecture rewrite"),
        ]

        for complexity, expected_strategy, description in test_cases:
            result = classifier.classify(description)
            assert result.complexity == complexity, f"Expected {complexity} but got {result.complexity} for: {description[:50]}"
            assert result.suggested_strategy == expected_strategy

    def test_estimated_tokens_matches_complexity(self, classifier):
        """Test that estimated tokens match complexity level."""
        test_cases = [
            (TaskComplexity.SIMPLE, 2000, "Simple quick fix with typo"),
            (TaskComplexity.MEDIUM, 6000, " ".join(["word"] * 50)),
            (TaskComplexity.COMPLEX, 20000, "Complex major architecture rewrite"),
        ]

        for complexity, expected_tokens, description in test_cases:
            result = classifier.classify(description)
            assert result.complexity == complexity, f"Expected {complexity} but got {result.complexity}"
            assert result.estimated_tokens == expected_tokens
