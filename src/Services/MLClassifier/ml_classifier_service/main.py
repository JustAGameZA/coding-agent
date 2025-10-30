"""Main FastAPI application for ML Classifier service."""

import logging
from contextlib import asynccontextmanager

from api.rate_limiter import limiter
from api.routes import classification, health, training
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from prometheus_fastapi_instrumentator import Instrumentator
from slowapi import _rate_limit_exceeded_handler
from slowapi.errors import RateLimitExceeded

# Configure logging
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Lifespan context manager for startup and shutdown events."""
    # Startup
    logger.info("Starting ML Classifier Service...")
    
    # Initialize database connection
    from infrastructure.database import init_db, close_db
    await init_db()
    
    # Start event consumer for TaskCompletedEvent
    from infrastructure.messaging import start_consumer, stop_consumer
    await start_consumer()
    
    logger.info("ML Classifier Service started successfully")

    yield

    # Shutdown
    logger.info("Shutting down ML Classifier Service...")
    
    # Stop event consumer
    await stop_consumer()
    
    # Close database connection
    await close_db()
    
    logger.info("ML Classifier Service shutdown complete")


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
app.include_router(training.router)

# Expose Prometheus metrics at /metrics
# Instrument after routers are included so all endpoints are covered
Instrumentator().instrument(app).expose(
    app, endpoint="/metrics", include_in_schema=False
)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True, log_level="info")
