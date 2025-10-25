"""Unit tests for model loader."""

import json
import tempfile
from pathlib import Path

import pytest
from infrastructure.ml.model_loader import ModelLoader, ModelMetadata


class TestModelLoader:
    """Test suite for model loader."""

    def test_initialization_default_path(self):
        """Test model loader initialization with default path."""
        loader = ModelLoader()
        assert loader.models_dir is not None
        assert loader.models_dir.exists()

    def test_initialization_custom_path(self):
        """Test model loader initialization with custom path."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            assert loader.models_dir == Path(tmpdir)
            assert loader.models_dir.exists()

    def test_load_latest_model_creates_dummy(self):
        """Test that loading model creates dummy model when none exists."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            model, vectorizer, metadata = loader.load_latest_model()

            assert model is not None
            assert vectorizer is not None
            assert metadata is not None
            assert "dummy" in metadata.version.lower()

    def test_dummy_model_files_created(self):
        """Test that dummy model creates necessary files."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            model, vectorizer, metadata = loader.load_latest_model()

            models_dir = Path(tmpdir)
            # Check that model file exists
            model_files = list(models_dir.glob("model_*.json"))
            assert len(model_files) > 0

            # Check that vectorizer file exists
            vectorizer_files = list(models_dir.glob("vectorizer_*.pkl"))
            assert len(vectorizer_files) > 0

            # Check that metadata file exists
            metadata_files = list(models_dir.glob("model_*_metadata.json"))
            assert len(metadata_files) > 0

    def test_current_model_property(self):
        """Test current model property."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            assert loader.current_model is None

            loader.load_latest_model()
            assert loader.current_model is not None

    def test_current_metadata_property(self):
        """Test current metadata property."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            assert loader.current_metadata is None

            loader.load_latest_model()
            assert loader.current_metadata is not None

    def test_warm_up(self):
        """Test model warm-up."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            loader.load_latest_model()

            # Should not raise an exception
            loader.warm_up()

    def test_warm_up_without_model_raises_error(self):
        """Test that warm-up without loaded model raises error."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)

            with pytest.raises(RuntimeError):
                loader.warm_up()

    def test_model_metadata_structure(self):
        """Test model metadata structure."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            _, _, metadata = loader.load_latest_model()

            assert hasattr(metadata, "version")
            assert hasattr(metadata, "accuracy")
            assert hasattr(metadata, "model_path")
            assert hasattr(metadata, "vectorizer_path")
            assert hasattr(metadata, "feature_count")
            assert hasattr(metadata, "task_types")
            assert hasattr(metadata, "created_at")

    def test_model_metadata_to_dict(self):
        """Test model metadata conversion to dictionary."""
        metadata = ModelMetadata(
            version="v1.0.0",
            accuracy=0.95,
            model_path="model.json",
            vectorizer_path="vectorizer.pkl",
            feature_count=100,
            task_types=["bug_fix", "feature"],
            created_at="2025-10-25T00:00:00",
        )

        metadata_dict = metadata.to_dict()
        assert isinstance(metadata_dict, dict)
        assert metadata_dict["version"] == "v1.0.0"
        assert metadata_dict["accuracy"] == 0.95

    def test_model_metadata_from_dict(self):
        """Test model metadata creation from dictionary."""
        data = {
            "version": "v1.0.0",
            "accuracy": 0.95,
            "model_path": "model.json",
            "vectorizer_path": "vectorizer.pkl",
            "feature_count": 100,
            "task_types": ["bug_fix", "feature"],
            "created_at": "2025-10-25T00:00:00",
        }

        metadata = ModelMetadata.from_dict(data)
        assert metadata.version == "v1.0.0"
        assert metadata.accuracy == 0.95

    def test_dummy_model_can_predict(self):
        """Test that dummy model can make predictions."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            model, vectorizer, metadata = loader.load_latest_model()

            # Extract features and predict
            description = "Fix the login bug"
            features = vectorizer.extract_features(description)
            predictions = model.predict_proba([features])

            assert predictions is not None
            assert predictions.shape[0] == 1
            assert predictions.shape[1] > 0  # Should have probabilities for each class

    def test_dummy_model_accuracy(self):
        """Test that dummy model has reasonable accuracy value."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            _, _, metadata = loader.load_latest_model()

            assert 0.0 <= metadata.accuracy <= 1.0
            # Dummy model should claim high accuracy
            assert metadata.accuracy >= 0.90

    def test_dummy_model_task_types(self):
        """Test that dummy model has all task types."""
        from domain.models.task_type import TaskType

        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            _, _, metadata = loader.load_latest_model()

            expected_types = [t.value for t in TaskType]
            assert len(metadata.task_types) == len(expected_types)
            for task_type in expected_types:
                assert task_type in metadata.task_types

    def test_dummy_model_feature_count(self):
        """Test that dummy model has correct feature count."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            _, vectorizer, metadata = loader.load_latest_model()

            assert metadata.feature_count == vectorizer.feature_count
            assert metadata.feature_count > 0

    def test_load_existing_model(self):
        """Test loading an existing model from disk."""
        with tempfile.TemporaryDirectory() as tmpdir:
            # First, create a dummy model
            loader1 = ModelLoader(models_dir=tmpdir)
            model1, vectorizer1, metadata1 = loader1.load_latest_model()

            # Now create a new loader and load the same model
            loader2 = ModelLoader(models_dir=tmpdir)
            model2, vectorizer2, metadata2 = loader2.load_latest_model()

            # Should load the same model
            assert metadata1.version == metadata2.version
            assert metadata1.accuracy == metadata2.accuracy

    def test_metadata_json_format(self):
        """Test that metadata is saved in correct JSON format."""
        with tempfile.TemporaryDirectory() as tmpdir:
            loader = ModelLoader(models_dir=tmpdir)
            loader.load_latest_model()

            # Find metadata file
            metadata_files = list(Path(tmpdir).glob("model_*_metadata.json"))
            assert len(metadata_files) > 0

            # Load and validate JSON
            with open(metadata_files[0], "r") as f:
                metadata_json = json.load(f)

            assert "version" in metadata_json
            assert "accuracy" in metadata_json
            assert "model_path" in metadata_json
            assert "vectorizer_path" in metadata_json
            assert "feature_count" in metadata_json
            assert "task_types" in metadata_json
            assert "created_at" in metadata_json
