"""Model loader for XGBoost models."""

import json
import pickle
from pathlib import Path
from typing import Any, Dict, Optional

import xgboost as xgb

from domain.models.task_type import TaskComplexity, TaskType


class ModelMetadata:
    """Metadata for an ML model."""

    def __init__(
        self,
        version: str,
        accuracy: float,
        model_path: str,
        vectorizer_path: str,
        feature_count: int,
        task_types: list[str],
        created_at: str,
    ):
        """Initialize model metadata."""
        self.version = version
        self.accuracy = accuracy
        self.model_path = model_path
        self.vectorizer_path = vectorizer_path
        self.feature_count = feature_count
        self.task_types = task_types
        self.created_at = created_at

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary."""
        return {
            "version": self.version,
            "accuracy": self.accuracy,
            "model_path": self.model_path,
            "vectorizer_path": self.vectorizer_path,
            "feature_count": self.feature_count,
            "task_types": self.task_types,
            "created_at": self.created_at,
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "ModelMetadata":
        """Create from dictionary."""
        return cls(**data)


class ModelLoader:
    """Load and manage XGBoost models."""

    def __init__(self, models_dir: Optional[Path] = None):
        """
        Initialize model loader.

        Args:
            models_dir: Directory containing model files
        """
        if models_dir is None:
            # Default to a models directory relative to this file
            self.models_dir = Path(__file__).parent.parent.parent / "models"
        else:
            self.models_dir = Path(models_dir)
        
        self.models_dir.mkdir(parents=True, exist_ok=True)
        
        self._current_model: Optional[xgb.XGBClassifier] = None
        self._current_vectorizer: Optional[Any] = None
        self._current_metadata: Optional[ModelMetadata] = None

    def load_latest_model(self) -> tuple[xgb.XGBClassifier, Any, ModelMetadata]:
        """
        Load the latest model version.

        Returns:
            Tuple of (model, vectorizer, metadata)
        """
        # Look for metadata files
        metadata_files = list(self.models_dir.glob("model_*.json"))
        
        if not metadata_files:
            # No models available, create a dummy model
            return self._create_dummy_model()
        
        # Sort by version (assuming format model_v{version}.json)
        metadata_files.sort(reverse=True)
        latest_metadata_file = metadata_files[0]
        
        # Load metadata
        with open(latest_metadata_file, "r") as f:
            metadata_dict = json.load(f)
            metadata = ModelMetadata.from_dict(metadata_dict)
        
        # Load model
        model_path = self.models_dir / metadata.model_path
        if model_path.suffix == ".json":
            model = xgb.XGBClassifier()
            model.load_model(str(model_path))
        else:
            with open(model_path, "rb") as f:
                model = pickle.load(f)
        
        # Load vectorizer
        vectorizer_path = self.models_dir / metadata.vectorizer_path
        with open(vectorizer_path, "rb") as f:
            vectorizer = pickle.load(f)
        
        self._current_model = model
        self._current_vectorizer = vectorizer
        self._current_metadata = metadata
        
        return model, vectorizer, metadata

    def _create_dummy_model(self) -> tuple[xgb.XGBClassifier, Any, ModelMetadata]:
        """
        Create a dummy model for initial testing.

        Returns:
            Tuple of (model, vectorizer, metadata)
        """
        from datetime import datetime

        import numpy as np
        from sklearn.feature_extraction.text import TfidfVectorizer

        from infrastructure.ml.feature_extractor import FeatureExtractor

        # Create sample training data
        task_types = [e.value for e in TaskType]
        complexity_levels = [e.value for e in TaskComplexity]
        
        # Sample descriptions for each task type
        training_samples = [
            ("Fix the login bug where users cannot authenticate", "bug_fix", "simple"),
            ("Error in payment processing needs to be fixed", "bug_fix", "medium"),
            ("Crash on startup when loading configuration", "bug_fix", "simple"),
            ("Implement user registration with email verification", "feature", "medium"),
            ("Add support for OAuth2 authentication", "feature", "complex"),
            ("Create a new dashboard for analytics", "feature", "complex"),
            ("Refactor the authentication module for better maintainability", "refactor", "medium"),
            ("Clean up the codebase and remove deprecated code", "refactor", "simple"),
            ("Optimize database queries for better performance", "refactor", "medium"),
            ("Write unit tests for the user service", "test", "medium"),
            ("Add integration tests with 90% coverage", "test", "complex"),
            ("Validate all edge cases in the API", "test", "medium"),
            ("Update the README with installation instructions", "documentation", "simple"),
            ("Document the API endpoints with examples", "documentation", "medium"),
            ("Create a comprehensive developer guide", "documentation", "complex"),
            ("Deploy the application to Kubernetes", "deployment", "complex"),
            ("Release the new version to production", "deployment", "medium"),
            ("Setup CI/CD pipeline with automated tests", "deployment", "complex"),
        ]
        
        # Extract features
        descriptions = [s[0] for s in training_samples]
        labels_type = [s[1] for s in training_samples]
        labels_complexity = [s[2] for s in training_samples]
        
        # Create and fit feature extractor
        feature_extractor = FeatureExtractor(max_features=100)
        feature_extractor.fit(descriptions)
        
        # Extract features for all samples
        X = np.array([feature_extractor.extract_features(desc) for desc in descriptions])
        
        # Encode labels
        type_to_idx = {t: i for i, t in enumerate(task_types)}
        y_type = np.array([type_to_idx[label] for label in labels_type])
        
        # Train a simple XGBoost classifier
        model = xgb.XGBClassifier(
            n_estimators=50,
            max_depth=3,
            learning_rate=0.1,
            objective="multi:softprob",
            num_class=len(task_types),
            random_state=42,
        )
        model.fit(X, y_type)
        
        # Save the model and vectorizer
        model_version = "v1.0.0-dummy"
        model_filename = f"model_{model_version}.json"
        vectorizer_filename = f"vectorizer_{model_version}.pkl"
        
        model_path = self.models_dir / model_filename
        vectorizer_path = self.models_dir / vectorizer_filename
        
        model.save_model(str(model_path))
        with open(vectorizer_path, "wb") as f:
            pickle.dump(feature_extractor, f)
        
        # Create metadata
        metadata = ModelMetadata(
            version=model_version,
            accuracy=0.95,  # Dummy accuracy
            model_path=model_filename,
            vectorizer_path=vectorizer_filename,
            feature_count=feature_extractor.feature_count,
            task_types=task_types,
            created_at=datetime.utcnow().isoformat(),
        )
        
        # Save metadata
        metadata_path = self.models_dir / f"model_{model_version}_metadata.json"
        with open(metadata_path, "w") as f:
            json.dump(metadata.to_dict(), f, indent=2)
        
        self._current_model = model
        self._current_vectorizer = feature_extractor
        self._current_metadata = metadata
        
        return model, feature_extractor, metadata

    @property
    def current_model(self) -> Optional[xgb.XGBClassifier]:
        """Get the currently loaded model."""
        return self._current_model

    @property
    def current_metadata(self) -> Optional[ModelMetadata]:
        """Get the current model metadata."""
        return self._current_metadata

    def warm_up(self) -> None:
        """Warm up the model by running a test prediction."""
        if self._current_model is None or self._current_vectorizer is None:
            raise RuntimeError("No model loaded. Call load_latest_model() first.")
        
        # Run a dummy prediction to warm up the model
        import numpy as np
        
        dummy_description = "Fix a bug in the authentication system"
        features = self._current_vectorizer.extract_features(dummy_description)
        _ = self._current_model.predict_proba([features])
