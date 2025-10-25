# ML Models Directory

This directory contains trained XGBoost models for task classification.

## Model Files

Each model version consists of three files:

1. **Model file**: `model_v{version}.json` - XGBoost model in JSON format
2. **Vectorizer file**: `vectorizer_v{version}.pkl` - Pickled feature extractor
3. **Metadata file**: `model_v{version}_metadata.json` - Model metadata

## Current Model

- **Version**: v1.0.0-dummy
- **Type**: Dummy model for testing and development
- **Accuracy**: 95% (simulated)
- **Features**: 122 features total
  - 100 TF-IDF features
  - 2 length features (char count, word count)
  - 6 keyword presence features (binary)
  - 6 keyword count features
  - 6 code pattern features
  - 2 complexity indicator features

## Model Metadata Format

```json
{
  "version": "v1.0.0-dummy",
  "accuracy": 0.95,
  "model_path": "model_v1.0.0-dummy.json",
  "vectorizer_path": "vectorizer_v1.0.0-dummy.pkl",
  "feature_count": 122,
  "task_types": [
    "bug_fix",
    "feature",
    "refactor",
    "test",
    "documentation",
    "deployment"
  ],
  "created_at": "2025-10-25T19:00:00.000000"
}
```

## Training New Models

To train a new model:

1. Collect training data from task execution feedback
2. Extract features using `FeatureExtractor`
3. Train XGBoost classifier
4. Save model, vectorizer, and metadata
5. Update version number

Example:

```python
from infrastructure.ml.feature_extractor import FeatureExtractor
from infrastructure.ml.model_loader import ModelLoader
import xgboost as xgb

# Load training data
descriptions = [...]  # List of task descriptions
labels = [...]  # List of task types (encoded as integers)

# Extract features
extractor = FeatureExtractor(max_features=1000)
extractor.fit(descriptions)
X = [extractor.extract_features(desc) for desc in descriptions]

# Train model
model = xgb.XGBClassifier(
    n_estimators=100,
    max_depth=6,
    learning_rate=0.1,
    objective='multi:softprob',
    num_class=6,
    random_state=42,
)
model.fit(X, labels)

# Save model
loader = ModelLoader()
# ... save model, vectorizer, and metadata
```

## Model Selection

The `ModelLoader` automatically loads the latest model version based on:

1. Metadata files in this directory
2. Lexicographic sorting of version strings
3. If no models exist, a dummy model is created automatically

## Performance Requirements

- **Target Accuracy**: ≥ 95%
- **Target Latency**: < 50ms average
- **Target Throughput**: ≥ 1000 req/s

Current dummy model performance:
- Accuracy: 95% (simulated)
- Latency: ~1.3ms average (40x faster than target)
- Throughput: ~870 req/s

## Future Improvements

1. **Real Training Data**: Replace dummy model with production-trained model
2. **Complexity Model**: Train separate model for complexity classification
3. **ONNX Export**: Convert to ONNX for faster inference
4. **A/B Testing**: Support multiple model versions for canary testing
5. **Auto-retraining**: Implement automatic retraining pipeline
