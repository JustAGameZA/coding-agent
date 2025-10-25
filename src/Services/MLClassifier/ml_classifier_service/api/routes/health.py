"""Health check and service status routes."""

import time
from typing import Optional

from fastapi import APIRouter
from pydantic import BaseModel


router = APIRouter(prefix="", tags=["Health"])


class DependencyStatus(BaseModel):
    """Dependency status model."""

    name: str
    status: str  # "healthy" | "unhealthy" | "unknown"
    latency_ms: Optional[float] = None
    message: Optional[str] = None


class HealthResponse(BaseModel):
    """Health check response model."""

    status: str
    service: str
    version: str
    dependencies: Optional[list[DependencyStatus]] = None


@router.get("/health", response_model=HealthResponse)
async def health_check() -> HealthResponse:
    """
    Health check endpoint with dependency status.

    Returns service status, version information, and health of dependencies.
    Dependencies checked:
    - Heuristic classifier (always available)
    - ML classifier (if configured)
    - LLM classifier (if configured)
    """
    dependencies = []

    # Check heuristic classifier (always healthy - no external dependencies)
    dependencies.append(
        DependencyStatus(
            name="heuristic_classifier",
            status="healthy",
            latency_ms=0.0,
            message="Keyword-based classifier ready",
        )
    )

    # Check ML classifier (currently optional, not yet configured in Phase 2)
    # In future phases, this would check if the ML model is loaded
    dependencies.append(
        DependencyStatus(
            name="ml_classifier",
            status="unknown",
            message="ML classifier not yet configured (Phase 2)",
        )
    )

    # Check LLM classifier (mock mode - no actual API calls for health check)
    # In production, would check LLM API connectivity
    dependencies.append(
        DependencyStatus(
            name="llm_classifier",
            status="healthy",
            message="LLM classifier ready (mock mode)",
        )
    )

    # Overall status is healthy if at least heuristic is available
    # (which is always the case)
    overall_status = "healthy"

    return HealthResponse(
        status=overall_status,
        service="ML Classifier",
        version="2.0.0",
        dependencies=dependencies,
    )


@router.get("/", response_model=dict)
async def root() -> dict:
    """
    Root endpoint with service information.
    
    Returns basic service metadata.
    """
    return {
        "service": "ML Classifier",
        "version": "2.0.0",
        "status": "running",
        "description": "Task classification service using heuristic and ML approaches"
    }
