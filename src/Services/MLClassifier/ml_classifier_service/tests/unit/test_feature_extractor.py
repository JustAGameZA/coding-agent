"""Unit tests for feature extractor."""

import numpy as np
import pytest
from infrastructure.ml.feature_extractor import FeatureExtractor


@pytest.fixture
def feature_extractor():
    """Create a feature extractor instance."""
    extractor = FeatureExtractor(max_features=100)
    # Fit with some sample data
    sample_descriptions = [
        "Fix the login bug where users can't authenticate",
        "Implement a new user registration feature",
        "Refactor the authentication module",
        "Write unit tests for the API",
        "Update the README documentation",
        "Deploy the application to production",
    ]
    extractor.fit(sample_descriptions)
    return extractor


class TestFeatureExtractor:
    """Test suite for feature extractor."""

    def test_initialization(self):
        """Test feature extractor initialization."""
        extractor = FeatureExtractor(max_features=50)
        assert extractor.max_features == 50
        assert not extractor._is_fitted

    def test_fit(self):
        """Test fitting the vectorizer."""
        extractor = FeatureExtractor(max_features=100)
        descriptions = [
            "Fix bug in authentication",
            "Add new feature for users",
            "Refactor code structure",
        ]
        extractor.fit(descriptions)
        assert extractor._is_fitted

    def test_extract_features_shape(self, feature_extractor):
        """Test that extracted features have correct shape."""
        description = "Fix the login bug"
        features = feature_extractor.extract_features(description)

        assert isinstance(features, np.ndarray)
        assert features.dtype == np.float32
        assert len(features) == feature_extractor.feature_count

    def test_tfidf_features(self, feature_extractor):
        """Test TF-IDF feature extraction."""
        description = "Fix the authentication bug in the login system"
        features = feature_extractor.extract_features(description)

        # Features should include TF-IDF features at the beginning
        # Note: actual TF-IDF feature count may be less than max_features
        # if training data has fewer unique terms
        assert len(features) > 0
        # At least some TF-IDF values should be non-zero (first 100 features are TF-IDF)
        tfidf_portion = features[:100]
        assert np.sum(tfidf_portion > 0) > 0

    def test_length_features(self, feature_extractor):
        """Test length feature extraction."""
        description = "Fix bug"
        features = feature_extractor.extract_features(description)
        feature_names = feature_extractor.get_feature_names()

        # Find length feature indices
        char_length_idx = feature_names.index("char_length")
        word_count_idx = feature_names.index("word_count")

        assert features[char_length_idx] == len(description)
        assert features[word_count_idx] == len(description.split())

    def test_keyword_presence_features(self, feature_extractor):
        """Test keyword presence binary features."""
        description = "Fix the bug in the authentication system"
        features = feature_extractor.extract_features(description)
        feature_names = feature_extractor.get_feature_names()

        # Should have bug keyword
        bug_keyword_idx = feature_names.index("has_bug_keyword")
        assert features[bug_keyword_idx] == 1.0

    def test_keyword_count_features(self, feature_extractor):
        """Test keyword count features."""
        description = "Fix the bug and error in the broken system"
        features = feature_extractor.extract_features(description)
        feature_names = feature_extractor.get_feature_names()

        # Should count bug-related keywords
        bug_count_idx = feature_names.index("bug_keyword_count")
        # "bug", "error", "broken" should all match
        assert features[bug_count_idx] >= 3.0

    def test_code_pattern_detection(self, feature_extractor):
        """Test code pattern detection features."""
        description = """
        Fix the bug in the `authenticate()` function:
        ```python
        def authenticate(user):
            return user.is_valid()
        ```
        """
        features = feature_extractor.extract_features(description)
        feature_names = feature_extractor.get_feature_names()

        # Should detect code block
        code_block_idx = feature_names.index("has_code_block")
        assert features[code_block_idx] == 1.0

        # Should detect inline code
        inline_code_idx = feature_names.index("has_inline_code")
        assert features[inline_code_idx] == 1.0

        # Should detect function definition
        function_def_idx = feature_names.index("has_function_def")
        assert features[function_def_idx] == 1.0

    def test_complexity_indicators(self, feature_extractor):
        """Test complexity indicator features."""
        simple_description = "Fix a small typo"
        complex_description = "Implement a complex microservices architecture"

        simple_features = feature_extractor.extract_features(simple_description)
        complex_features = feature_extractor.extract_features(complex_description)

        feature_names = feature_extractor.get_feature_names()
        simple_idx = feature_names.index("simple_keyword_count")
        complex_idx = feature_names.index("complex_keyword_count")

        # Simple description should have simple keywords
        assert simple_features[simple_idx] >= 1.0
        # Complex description should have complex keywords
        assert complex_features[complex_idx] >= 1.0

    def test_feature_names(self, feature_extractor):
        """Test that feature names are correctly generated."""
        feature_names = feature_extractor.get_feature_names()

        assert len(feature_names) == feature_extractor.feature_count
        assert "char_length" in feature_names
        assert "word_count" in feature_names
        assert any("tfidf_" in name for name in feature_names)
        assert any("keyword" in name for name in feature_names)

    def test_feature_count_property(self, feature_extractor):
        """Test feature count property."""
        count = feature_extractor.feature_count
        assert count > 0
        assert count == len(feature_extractor.get_feature_names())

    def test_consistency_across_calls(self, feature_extractor):
        """Test that same input produces same features."""
        description = "Fix the authentication bug"

        features1 = feature_extractor.extract_features(description)
        features2 = feature_extractor.extract_features(description)

        np.testing.assert_array_equal(features1, features2)

    def test_different_inputs_different_features(self, feature_extractor):
        """Test that different inputs produce different features."""
        description1 = "Fix a bug"
        description2 = "Add a feature"

        features1 = feature_extractor.extract_features(description1)
        features2 = feature_extractor.extract_features(description2)

        # Features should be different
        assert not np.array_equal(features1, features2)

    def test_empty_description(self, feature_extractor):
        """Test handling of empty description."""
        description = ""
        features = feature_extractor.extract_features(description)

        assert isinstance(features, np.ndarray)
        assert len(features) == feature_extractor.feature_count

    def test_very_long_description(self, feature_extractor):
        """Test handling of very long description."""
        description = " ".join(["word"] * 1000)
        features = feature_extractor.extract_features(description)

        assert isinstance(features, np.ndarray)
        assert len(features) == feature_extractor.feature_count

    def test_special_characters(self, feature_extractor):
        """Test handling of special characters."""
        description = "Fix bug with special chars: @#$%^&*()"
        features = feature_extractor.extract_features(description)

        assert isinstance(features, np.ndarray)
        assert len(features) == feature_extractor.feature_count

    def test_unicode_characters(self, feature_extractor):
        """Test handling of unicode characters."""
        description = "Fix bug in système d'authentification"
        features = feature_extractor.extract_features(description)

        assert isinstance(features, np.ndarray)
        assert len(features) == feature_extractor.feature_count

    def test_multiple_code_blocks(self, feature_extractor):
        """Test detection of multiple code blocks."""
        description = """
        Fix bugs in these functions:
        ```python
        def func1():
            pass
        ```
        And also:
        ```python
        def func2():
            pass
        ```
        """
        features = feature_extractor.extract_features(description)
        feature_names = feature_extractor.get_feature_names()

        code_block_idx = feature_names.index("has_code_block")
        # Should detect at least one code block
        assert features[code_block_idx] == 1.0

    def test_all_task_type_keywords(self, feature_extractor):
        """Test that all task type keyword categories work."""
        test_cases = [
            ("Fix the bug", "has_bug_keyword"),
            ("Add new feature", "has_feature_keyword"),
            ("Refactor code", "has_refactor_keyword"),
            ("Write tests", "has_test_keyword"),
            ("Update documentation", "has_documentation_keyword"),
            ("Deploy application", "has_deployment_keyword"),
        ]

        for description, expected_feature in test_cases:
            features = feature_extractor.extract_features(description)
            feature_names = feature_extractor.get_feature_names()
            feature_idx = feature_names.index(expected_feature)
            assert (
                features[feature_idx] == 1.0
            ), f"Failed for: {description} -> {expected_feature}"
