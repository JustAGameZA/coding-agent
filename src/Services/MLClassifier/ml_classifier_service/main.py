"""Main FastAPI application for ML Classifier service."""

import logging
from contextlib import asynccontextmanager

from api.routes import classification, health
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from prometheus_fastapi_instrumentator import Instrumentator
from slowapi import Limiter, _rate_limit_exceeded_handler
from slowapi.errors import RateLimitExceeded
from slowapi.util import get_remote_address

# Configure logging
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

# Initialize rate limiter (100 requests per minute per IP)
limiter = Limiter(key_func=get_remote_address, default_limits=["100/minute"])


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Lifespan context manager for startup and shutdown events."""
    # Startup
    logger.info("Starting ML Classifier Service...")
    logger.info("Heuristic classifier initialized")
    # Future: Load ML models, connect to database, start event consumers

    yield

    # Shutdown
    logger.info("Shutting down ML Classifier Service...")
    # Future: Close database connections, stop event consumers


# Create FastAPI app
app = FastAPI(
    title="ML Classifier Service",
    description="Task classification service using heuristic and ML approaches",
    version="2.0.0",
    lifespan=lifespan,
)

# Add rate limiter state and exception handler
app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routers
app.include_router(health.router)
app.include_router(classification.router)

# Expose Prometheus metrics at /metrics
# Instrument after routers are included so all endpoints are covered
Instrumentator().instrument(app).expose(
    app, endpoint="/metrics", include_in_schema=False
)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True, log_level="info")
