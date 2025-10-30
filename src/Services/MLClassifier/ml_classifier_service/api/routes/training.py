"""Training routes for ML model retraining and feedback collection."""

import logging
from typing import Optional

from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel, Field

from infrastructure.database import get_training_repo

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/train", tags=["Training"])


class FeedbackRequest(BaseModel):
    """Feedback for a classification result."""

    task_description: str = Field(
        ..., min_length=10, max_length=10000, description="Task description"
    )
    predicted_type: str = Field(..., description="Predicted task type")
    predicted_complexity: str = Field(..., description="Predicted complexity")
    actual_type: Optional[str] = Field(None, description="Actual task type")
    actual_complexity: Optional[str] = Field(None, description="Actual complexity")
    confidence: float = Field(..., ge=0.0, le=1.0, description="Prediction confidence")
    classifier_used: str = Field(..., description="Which classifier was used")
    was_correct: Optional[bool] = Field(
        None, description="Whether prediction was correct"
    )


class TrainingRequest(BaseModel):
    """Request to trigger model retraining."""

    min_samples: int = Field(
        1000, ge=100, le=100000, description="Minimum samples required for training"
    )
    model_version: Optional[str] = Field(
        None, description="Version tag for new model"
    )


class TrainingResponse(BaseModel):
    """Response from training request."""

    status: str
    message: str
    samples_used: Optional[int] = None
    new_model_version: Optional[str] = None
    accuracy: Optional[float] = None


@router.post("/feedback", status_code=status.HTTP_201_CREATED)
async def submit_feedback(feedback: FeedbackRequest) -> dict:
    """
    Submit classification feedback for model improvement.

    This endpoint allows users or services to provide feedback on classification
    results, which is stored for future model retraining.

    Args:
        feedback: Classification feedback data

    Returns:
        Success confirmation message

    Raises:
        HTTPException: If feedback storage fails
    """
    try:
        logger.info(
            f"Receiving feedback: type={feedback.predicted_type}, "
            f"classifier={feedback.classifier_used}, confidence={feedback.confidence:.2f}"
        )

        # Store feedback in database
        repo = get_training_repo()
        feedback_data = {
            "task_description": feedback.task_description,
            "predicted_type": feedback.predicted_type,
            "predicted_complexity": feedback.predicted_complexity,
            "actual_type": feedback.actual_type,
            "actual_complexity": feedback.actual_complexity,
            "confidence": feedback.confidence,
            "classifier_used": feedback.classifier_used,
            "was_correct": feedback.was_correct,
        }

        await repo.store_feedback(feedback_data)

        logger.info("Feedback stored successfully")
        return {
            "status": "success",
            "message": "Feedback received and stored for future training",
        }

    except Exception as e:
        logger.error(f"Failed to store feedback: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to store feedback: {str(e)}",
        )


@router.post("/retrain", response_model=TrainingResponse)
async def retrain_model(request: TrainingRequest) -> TrainingResponse:
    """
    Trigger model retraining with collected feedback.

    This endpoint initiates an asynchronous model retraining process using
    the accumulated feedback data. The training process includes:
    1. Fetching training samples from database
    2. Feature extraction and preprocessing
    3. Model training with cross-validation
    4. Model evaluation
    5. Model versioning and persistence

    Note: This is a long-running operation that may take several minutes.
    In production, this should be implemented as an async task queue job.

    Args:
        request: Training configuration parameters

    Returns:
        Training status and new model information

    Raises:
        HTTPException: If training fails or insufficient samples
    """
    try:
        logger.info(
            f"Retraining request received: min_samples={request.min_samples}, "
            f"version={request.model_version}"
        )

        # Get training data
        repo = get_training_repo()
        training_samples = await repo.get_training_samples(limit=request.min_samples)

        if len(training_samples) < request.min_samples:
            logger.warning(
                f"Insufficient training samples: {len(training_samples)} < {request.min_samples}"
            )
            return TrainingResponse(
                status="insufficient_data",
                message=f"Need at least {request.min_samples} samples, only have {len(training_samples)}",
                samples_used=len(training_samples),
            )

        # Implement actual retraining logic
        logger.info(f"Starting model retraining with {len(training_samples)} samples")
        
        # 1. Prepare training dataset
        descriptions = [sample['task_description'] for sample in training_samples]
        task_types = [sample['task_type'] for sample in training_samples]
        complexities = [sample['complexity'] for sample in training_samples]
        
        # 2. Import required libraries
        import numpy as np
        import xgboost as xgb
        from sklearn.feature_extraction.text import TfidfVectorizer
        from sklearn.model_selection import train_test_split
        from sklearn.preprocessing import LabelEncoder
        from sklearn.metrics import accuracy_score, precision_recall_fscore_support
        import joblib
        import os
        from datetime import datetime
        
        # 3. Encode labels
        type_encoder = LabelEncoder()
        complexity_encoder = LabelEncoder()
        
        encoded_types = type_encoder.fit_transform(task_types)
        encoded_complexities = complexity_encoder.fit_transform(complexities)
        
        # 4. Feature extraction
        vectorizer = TfidfVectorizer(
            max_features=1000,
            stop_words='english',
            ngram_range=(1, 2),
            min_df=2
        )
        X = vectorizer.fit_transform(descriptions).toarray()
        
        # 5. Split data for type classification
        X_train, X_test, y_train, y_test = train_test_split(
            X, encoded_types, test_size=0.2, random_state=42, stratify=encoded_types
        )
        
        # 6. Train XGBoost model for task type
        model = xgb.XGBClassifier(
            n_estimators=100,
            max_depth=6,
            learning_rate=0.1,
            objective='multi:softprob',
            num_class=len(type_encoder.classes_),
            random_state=42,
            eval_metric='mlogloss'
        )
        
        model.fit(X_train, y_train)
        
        # 7. Evaluate model
        y_pred = model.predict(X_test)
        accuracy = accuracy_score(y_test, y_pred)
        precision, recall, f1, _ = precision_recall_fscore_support(y_test, y_pred, average='macro')
        
        logger.info(f"Model evaluation - Accuracy: {accuracy:.4f}, F1: {f1:.4f}")
        
        # 8. Save model with versioning
        version = request.model_version or f"v{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        model_dir = "models/trained"
        os.makedirs(model_dir, exist_ok=True)
        
        model_path = f"{model_dir}/xgboost_model_{version}.json"
        vectorizer_path = f"{model_dir}/vectorizer_{version}.pkl"
        encoders_path = f"{model_dir}/encoders_{version}.pkl"
        
        # Save model in XGBoost format
        model.save_model(model_path)
        
        # Save vectorizer and encoders
        joblib.dump(vectorizer, vectorizer_path)
        joblib.dump({
            'type_encoder': type_encoder,
            'complexity_encoder': complexity_encoder
        }, encoders_path)
        
        # 9. Save model metadata to database
        model_metadata = {
            'version': version,
            'model_path': model_path,
            'vectorizer_path': vectorizer_path,
            'accuracy': float(accuracy),
            'precision_macro': float(precision),
            'recall_macro': float(recall),
            'f1_macro': float(f1),
            'training_samples': len(training_samples),
            'is_active': True  # Mark as active model
        }
        
        await repo.save_model_version(model_metadata)
        
        logger.info(f"Model retraining completed successfully - version: {version}")
        
        return TrainingResponse(
            status="completed",
            message=f"Model retrained successfully with {len(training_samples)} samples",
            samples_used=len(training_samples),
            new_model_version=version,
            accuracy=accuracy,
        )

    except Exception as e:
        logger.error(f"Model retraining failed: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Model retraining failed: {str(e)}",
        )


@router.get("/stats")
async def get_training_stats() -> dict:
    """
    Get statistics about collected training data.

    Returns information about:
    - Total feedback samples collected
    - Distribution by classifier type
    - Distribution by task type
    - Average confidence scores
    - Accuracy metrics (when actual results provided)

    Returns:
        Dictionary with training data statistics

    Raises:
        HTTPException: If stats retrieval fails
    """
    try:
        logger.info("Training stats requested")

        # TODO: Phase 2 - Query actual stats from database
        # For now, return placeholder data

        return {
            "total_samples": 0,
            "samples_with_feedback": 0,
            "distribution": {
                "heuristic": 0,
                "ml": 0,
                "llm": 0,
            },
            "average_confidence": 0.0,
            "accuracy": None,
            "message": "Training stats will be implemented in Phase 2",
        }

    except Exception as e:
        logger.error(f"Failed to get training stats: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to get training stats: {str(e)}",
        )
