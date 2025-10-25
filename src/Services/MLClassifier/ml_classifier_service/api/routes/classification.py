"""Classification routes for task classification API."""

import logging

from api.schemas.classification import ClassificationRequest, ClassificationResult
from domain.classifiers.hybrid import HybridClassifier
from fastapi import APIRouter, HTTPException, status

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/classify", tags=["Classification"])

# Initialize the hybrid classifier (singleton)
# This uses heuristic -> ML -> LLM cascade with circuit breaker
hybrid_classifier = HybridClassifier()


@router.post("/", response_model=ClassificationResult)
async def classify_task(request: ClassificationRequest) -> ClassificationResult:
    """
    Classify a coding task using hybrid approach.

    Uses three-tier hybrid classification:
    1. Heuristic (fast, 90% accuracy) - confidence >= 0.85
    2. ML (medium, 95% accuracy) - confidence >= 0.70
    3. LLM (slow, 98% accuracy) - fallback for low confidence

    Returns task type, complexity, confidence, and execution recommendations.

    Args:
        request: Classification request with task description

    Returns:
        Classification result with task type, complexity, and recommendations

    Raises:
        HTTPException: If classification fails
    """
    try:
        logger.info(f"Classifying task: {request.task_description[:50]}...")

        # Use hybrid classifier (heuristic -> ML -> LLM cascade)
        result = await hybrid_classifier.classify(request)

        logger.info(
            f"Classification complete: type={result.task_type.value}, "
            f"complexity={result.complexity.value}, confidence={result.confidence:.2f}, "
            f"classifier={result.classifier_used}"
        )

        return result

    except Exception as e:
        logger.error(f"Classification failed: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Classification failed: {str(e)}",
        )


@router.post("/batch", response_model=list[ClassificationResult])
async def classify_tasks_batch(
    requests: list[ClassificationRequest],
) -> list[ClassificationResult]:
    """
    Classify multiple tasks in batch using hybrid approach.

    Args:
        requests: List of classification requests

    Returns:
        List of classification results

    Raises:
        HTTPException: If batch classification fails
    """
    try:
        logger.info(f"Batch classifying {len(requests)} tasks...")

        results = []
        for request in requests:
            result = await hybrid_classifier.classify(request)
            results.append(result)

        logger.info(f"Batch classification complete: {len(results)} tasks processed")

        return results

    except Exception as e:
        logger.error(f"Batch classification failed: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Batch classification failed: {str(e)}",
        )


@router.get("/metrics")
async def get_classification_metrics():
    """
    Get classification metrics.

    Returns metrics about classifier usage, latency, and performance.
    This includes:
    - Total classifications
    - Distribution across heuristic/ML/LLM classifiers
    - Average latency
    - Circuit breaker trips
    - Timeout counts

    Returns:
        Dictionary with comprehensive metrics
    """
    try:
        metrics = hybrid_classifier.get_metrics()
        logger.info("Metrics requested")
        return metrics

    except Exception as e:
        logger.error(f"Failed to get metrics: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to get metrics: {str(e)}",
        )


@router.post("/metrics/reset")
async def reset_classification_metrics():
    """
    Reset classification metrics.

    Resets all metric counters to zero. Useful for testing or
    after monitoring system updates.

    Returns:
        Success message
    """
    try:
        hybrid_classifier.reset_metrics()
        logger.info("Metrics reset")
        return {"message": "Metrics reset successfully"}

    except Exception as e:
        logger.error(f"Failed to reset metrics: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to reset metrics: {str(e)}",
        )


@router.post("/circuit-breaker/reset")
async def reset_circuit_breaker():
    """
    Manually reset the LLM circuit breaker.

    Forces the circuit breaker to close, allowing LLM calls to be attempted
    again. Use this if you know the LLM service has recovered.

    Returns:
        Success message
    """
    try:
        hybrid_classifier.reset_circuit_breaker()
        logger.info("Circuit breaker reset")
        return {"message": "Circuit breaker reset successfully"}

    except Exception as e:
        logger.error(f"Failed to reset circuit breaker: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to reset circuit breaker: {str(e)}",
        )
