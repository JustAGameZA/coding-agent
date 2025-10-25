"""Pydantic schemas for classification API."""

from typing import Optional

from domain.models.task_type import TaskComplexity, TaskType
from pydantic import BaseModel, Field


class ClassificationRequest(BaseModel):
    """Request model for task classification."""

    task_description: str = Field(
        ..., min_length=10, max_length=10000, description="Description of the coding task"
    )
    context: Optional[dict[str, str]] = Field(
        None, description="Additional context for classification"
    )
    files_changed: Optional[list[str]] = Field(
        None, description="List of file paths that will be changed"
    )

    model_config = {
        "json_schema_extra": {
            "examples": [
                {
                    "task_description": "Fix the login bug where users can't authenticate",
                    "context": {"repository": "backend-api"},
                    "files_changed": ["src/auth/login.py"],
                }
            ]
        }
    }


class ClassificationResult(BaseModel):
    """Result model for task classification."""

    task_type: TaskType = Field(..., description="Classified type of the task")
    complexity: TaskComplexity = Field(..., description="Estimated complexity level")
    confidence: float = Field(
        ..., ge=0.0, le=1.0, description="Confidence score (0.0 - 1.0)"
    )
    reasoning: str = Field(
        ..., description="Explanation of the classification decision"
    )
    suggested_strategy: str = Field(..., description="Recommended execution strategy")
    estimated_tokens: int = Field(
        ..., gt=0, description="Estimated token count for execution"
    )
    classifier_used: Optional[str] = Field(
        None, description="Which classifier was used (heuristic/ml/llm)"
    )

    model_config = {
        "json_schema_extra": {
            "examples": [
                {
                    "task_type": "bug_fix",
                    "complexity": "simple",
                    "confidence": 0.92,
                    "reasoning": "Matched 3 keywords for bug_fix: 'fix', 'bug', 'authenticate'",
                    "suggested_strategy": "SingleShot",
                    "estimated_tokens": 2000,
                    "classifier_used": "heuristic",
                }
            ]
        }
    }
