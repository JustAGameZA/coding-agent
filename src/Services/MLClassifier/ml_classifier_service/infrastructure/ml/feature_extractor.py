"""Feature extraction for ML classification."""

import re
from typing import Dict, List

import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer


class FeatureExtractor:
    """Extract features from task descriptions for ML classification."""

    def __init__(self, max_features: int = 1000):
        """
        Initialize feature extractor.

        Args:
            max_features: Maximum number of TF-IDF features to extract
        """
        self.max_features = max_features
        self.vectorizer = TfidfVectorizer(
            max_features=max_features,
            stop_words="english",
            ngram_range=(1, 2),  # Unigrams and bigrams
            lowercase=True,
            min_df=1,
        )
        
        # Pre-defined keyword categories for binary features
        self.keyword_categories = {
            "bug": [
                "bug", "error", "fix", "crash", "issue", "fail", "broken",
                "defect", "problem", "incorrect"
            ],
            "feature": [
                "add", "implement", "create", "new", "feature", "enhance",
                "support", "introduce", "extend", "build"
            ],
            "refactor": [
                "refactor", "clean", "optimize", "improve", "reorganize",
                "restructure", "simplify", "modernize", "upgrade"
            ],
            "test": [
                "test", "tests", "testing", "unit test", "unit tests", 
                "integration test", "integration tests", "coverage", "spec",
                "validate", "verify", "mock", "mocking", "assertion"
            ],
            "documentation": [
                "doc", "documentation", "readme", "comment", "explain",
                "describe", "guide", "tutorial", "example", "annotate"
            ],
            "deployment": [
                "deploy", "release", "ci/cd", "pipeline", "docker",
                "kubernetes", "helm", "container", "infrastructure"
            ],
        }
        
        # Code pattern regexes
        self.code_patterns = [
            (r"```[\w]*[\s\S]*?```", "code_block"),  # Code blocks (more flexible)
            (r"`[^`]+`", "inline_code"),  # Inline code
            (r"\b[a-z_]+\.[a-z_]+\(", "function_call"),  # Function calls
            (r"class\s+\w+", "class_def"),  # Class definitions
            (r"def\s+\w+", "function_def"),  # Function definitions
            (r"import\s+\w+", "import_statement"),  # Import statements
        ]
        
        self._is_fitted = False

    def fit(self, task_descriptions: List[str]) -> "FeatureExtractor":
        """
        Fit the TF-IDF vectorizer on training data.

        Args:
            task_descriptions: List of task descriptions for training

        Returns:
            Self for method chaining
        """
        self.vectorizer.fit(task_descriptions)
        self._is_fitted = True
        return self

    def extract_features(self, task_description: str) -> np.ndarray:
        """
        Extract all features from a task description.

        Args:
            task_description: The task description text

        Returns:
            Feature vector as numpy array
        """
        features = []
        
        # 1. TF-IDF features (max 1000 features)
        if self._is_fitted:
            tfidf_features = self.vectorizer.transform([task_description]).toarray()[0]
            # Pad or truncate to max_features
            if len(tfidf_features) < self.max_features:
                # Pad with zeros
                tfidf_features = np.pad(
                    tfidf_features,
                    (0, self.max_features - len(tfidf_features)),
                    mode='constant',
                    constant_values=0
                )
            else:
                # Truncate
                tfidf_features = tfidf_features[:self.max_features]
        else:
            # If not fitted, use zero vector
            tfidf_features = np.zeros(self.max_features)
        features.extend(tfidf_features)
        
        # 2. Length features
        features.append(len(task_description))  # Character count
        features.append(len(task_description.split()))  # Word count
        
        # 3. Keyword presence (binary features for each category)
        for category, keywords in self.keyword_categories.items():
            has_keyword = any(
                re.search(rf"\b{re.escape(kw)}\b", task_description, re.IGNORECASE)
                for kw in keywords
            )
            features.append(1.0 if has_keyword else 0.0)
        
        # 4. Keyword counts
        for category, keywords in self.keyword_categories.items():
            count = sum(
                len(re.findall(rf"\b{re.escape(kw)}\b", task_description, re.IGNORECASE))
                for kw in keywords
            )
            features.append(float(count))
        
        # 5. Code snippet detection (binary features)
        for pattern, name in self.code_patterns:
            has_pattern = bool(re.search(pattern, task_description, re.MULTILINE | re.DOTALL))
            features.append(1.0 if has_pattern else 0.0)
        
        # 6. Complexity indicators
        # Check for complexity keywords
        simple_keywords = ["small", "quick", "minor", "trivial", "typo", "one-line", "simple"]
        complex_keywords = ["complex", "major", "architecture", "rewrite", "migration", "large-scale"]
        
        simple_count = sum(
            len(re.findall(rf"\b{re.escape(kw)}\b", task_description, re.IGNORECASE))
            for kw in simple_keywords
        )
        complex_count = sum(
            len(re.findall(rf"\b{re.escape(kw)}\b", task_description, re.IGNORECASE))
            for kw in complex_keywords
        )
        
        features.append(float(simple_count))
        features.append(float(complex_count))
        
        return np.array(features, dtype=np.float32)

    def get_feature_names(self) -> List[str]:
        """
        Get names of all features.

        Returns:
            List of feature names
        """
        names = []
        
        # TF-IDF feature names (always max_features count due to padding)
        if self._is_fitted:
            actual_names = list(self.vectorizer.get_feature_names_out())
            # Pad to max_features
            for i in range(self.max_features):
                if i < len(actual_names):
                    names.append(f"tfidf_{actual_names[i]}")
                else:
                    names.append(f"tfidf_padding_{i}")
        else:
            names.extend([f"tfidf_{i}" for i in range(self.max_features)])
        
        # Length features
        names.extend(["char_length", "word_count"])
        
        # Keyword presence features
        for category in self.keyword_categories.keys():
            names.append(f"has_{category}_keyword")
        
        # Keyword count features
        for category in self.keyword_categories.keys():
            names.append(f"{category}_keyword_count")
        
        # Code pattern features
        for _, name in self.code_patterns:
            names.append(f"has_{name}")
        
        # Complexity features
        names.extend(["simple_keyword_count", "complex_keyword_count"])
        
        return names

    @property
    def feature_count(self) -> int:
        """Get total number of features."""
        return len(self.get_feature_names())
