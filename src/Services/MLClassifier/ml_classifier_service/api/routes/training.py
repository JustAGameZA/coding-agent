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

        # TODO: Phase 2 - Implement actual retraining logic
        # 1. Prepare training dataset
        # 2. Split into train/validation sets
        # 3. Train XGBoost model
        # 4. Evaluate on validation set
        # 5. Save model with versioning
        # 6. Update model loader to use new model

        logger.info(
            f"Model retraining placeholder: would train on {len(training_samples)} samples"
        )

        return TrainingResponse(
            status="pending",
            message="Model retraining will be implemented in Phase 2",
            samples_used=len(training_samples),
            new_model_version=request.model_version or "v1.0.0-pending",
            accuracy=None,
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
