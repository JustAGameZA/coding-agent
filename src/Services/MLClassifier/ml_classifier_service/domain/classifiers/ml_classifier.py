"""ML-based classifier using XGBoost for task classification."""

from typing import Optional

import numpy as np
import xgboost as xgb

from api.schemas.classification import ClassificationRequest, ClassificationResult
from domain.models.task_type import TaskComplexity, TaskType
from infrastructure.ml.feature_extractor import FeatureExtractor
from infrastructure.ml.model_loader import ModelLoader, ModelMetadata


class MLClassifier:
    """ML-based classification using XGBoost (95% accuracy, 50ms latency)."""

    # Confidence threshold for using ML results vs. falling back to LLM
    ML_CONFIDENCE_THRESHOLD = 0.70

    def __init__(
        self,
        model: Optional[xgb.XGBClassifier] = None,
        feature_extractor: Optional[FeatureExtractor] = None,
        metadata: Optional[ModelMetadata] = None,
    ):
        """
        Initialize ML classifier.

        Args:
            model: Trained XGBoost model (if None, will load from disk)
            feature_extractor: Feature extractor instance (if None, will load from disk)
            metadata: Model metadata (if None, will load from disk)
        """
        if model is None or feature_extractor is None or metadata is None:
            # Load model from disk
            loader = ModelLoader()
            self.model, self.feature_extractor, self.metadata = (
                loader.load_latest_model()
            )
        else:
            self.model = model
            self.feature_extractor = feature_extractor
            self.metadata = metadata

        # Task type mapping (index to TaskType)
        self.task_types = [TaskType(t) for t in self.metadata.task_types]

    async def classify(
        self, request: ClassificationRequest
    ) -> ClassificationResult:
        """
        Classify a task using the ML model.

        Args:
            request: Classification request with task description

        Returns:
            ClassificationResult with predictions and confidence
        """
        # Extract features
        features = self.feature_extractor.extract_features(
            request.task_description
        )

        # Get predictions with probabilities
        probabilities = self.model.predict_proba([features])[0]
        predicted_idx = int(np.argmax(probabilities))
        confidence = float(probabilities[predicted_idx])

        # Map prediction to task type
        predicted_type = self.task_types[predicted_idx]

        # Classify complexity (can be improved with a separate model)
        complexity = self._classify_complexity(request.task_description, features)

        # Build reasoning
        top_3_indices = np.argsort(probabilities)[-3:][::-1]
        top_3_predictions = [
            (self.task_types[i].value, float(probabilities[i]))
            for i in top_3_indices
        ]
        reasoning = (
            f"ML model (v{self.metadata.version}) predicted {predicted_type.value} "
            f"with {confidence:.2%} confidence. "
            f"Top predictions: {', '.join(f'{t}={p:.2%}' for t, p in top_3_predictions)}"
        )

        return ClassificationResult(
            task_type=predicted_type,
            complexity=complexity,
            confidence=confidence,
            reasoning=reasoning,
            suggested_strategy=self._suggest_strategy(complexity),
            estimated_tokens=self._estimate_tokens(complexity),
            classifier_used="ml",
        )

    def _classify_complexity(
        self, description: str, features: np.ndarray
    ) -> TaskComplexity:
        """
        Classify task complexity.

        For now, uses heuristics. In the future, could use a separate model.

        Args:
            description: Task description
            features: Extracted feature vector

        Returns:
            TaskComplexity enum value
        """
        import re
        
        # Check for explicit complexity keywords
        complex_keywords = ["complex", "major", "architecture", "rewrite", "migration", "large-scale", "entire", "system-wide"]
        simple_keywords = ["small", "quick", "minor", "trivial", "typo", "one-line", "simple"]
        
        # Count complexity keyword matches
        complex_matches = sum(
            1 for kw in complex_keywords
            if re.search(rf"\b{re.escape(kw)}\b", description, re.IGNORECASE)
        )
        simple_matches = sum(
            1 for kw in simple_keywords
            if re.search(rf"\b{re.escape(kw)}\b", description, re.IGNORECASE)
        )
        
        # If explicit complexity keywords are present, use them
        if complex_matches > 0 and complex_matches > simple_matches:
            return TaskComplexity.COMPLEX
        elif simple_matches > 0 and simple_matches > complex_matches:
            return TaskComplexity.SIMPLE
        
        # Otherwise, use word count heuristic
        word_count = len(description.split())
        if word_count < 20:
            return TaskComplexity.SIMPLE
        elif word_count > 100:
            return TaskComplexity.COMPLEX
        else:
            return TaskComplexity.MEDIUM

    def _suggest_strategy(self, complexity: TaskComplexity) -> str:
        """
        Suggest execution strategy based on complexity.

        Args:
            complexity: Task complexity level

        Returns:
            Strategy name as string
        """
        return {
            TaskComplexity.SIMPLE: "SingleShot",
            TaskComplexity.MEDIUM: "Iterative",
            TaskComplexity.COMPLEX: "MultiAgent",
        }[complexity]

    def _estimate_tokens(self, complexity: TaskComplexity) -> int:
        """
        Estimate token usage based on complexity.

        Args:
            complexity: Task complexity level

        Returns:
            Estimated token count
        """
        return {
            TaskComplexity.SIMPLE: 2000,
            TaskComplexity.MEDIUM: 6000,
            TaskComplexity.COMPLEX: 20000,
        }[complexity]

    def get_model_version(self) -> str:
        """Get the version of the currently loaded model."""
        return self.metadata.version

    def get_model_accuracy(self) -> float:
        """Get the accuracy of the currently loaded model."""
        return self.metadata.accuracy

    @classmethod
    def load_from_disk(cls) -> "MLClassifier":
        """
        Load model from disk and create classifier instance.

        Returns:
            MLClassifier instance with loaded model
        """
        loader = ModelLoader()
        model, feature_extractor, metadata = loader.load_latest_model()

        # Warm up the model
        loader.warm_up()

        return cls(model=model, feature_extractor=feature_extractor, metadata=metadata)
