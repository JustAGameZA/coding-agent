"""Database connection and repository infrastructure.

Implements PostgreSQL connection management and training data repository
for ML model improvement and feedback collection.
"""

import json
import logging
from datetime import datetime
from typing import Dict, List, Optional

import asyncpg
from asyncpg import Pool

logger = logging.getLogger(__name__)


# Placeholder for future database connection management
class DatabaseConnection:
    """Manages database connections using asyncpg."""
    
    def __init__(self, connection_string: Optional[str] = None):
        self.connection_string = connection_string or (
            "postgresql://postgres:devPassword123!@localhost:5432/coding_agent_ml"
        )
        self._pool: Optional[Pool] = None
    
    async def connect(self):
        """Initialize database connection pool."""
        try:
            logger.info(f"Connecting to PostgreSQL database...")
            
            self._pool = await asyncpg.create_pool(
                self.connection_string,
                min_size=2,
                max_size=10,
                command_timeout=30
            )
            
            # Create tables if they don't exist
            await self._create_tables()
            logger.info("Connected to PostgreSQL successfully")
            
        except Exception as e:
            logger.error(f"Failed to connect to PostgreSQL: {e}")
            logger.warning("ML Classifier will continue without database persistence")
            # Don't raise - allow service to start without database
    
    async def disconnect(self):
        """Close database connection pool."""
        if self._pool:
            await self._pool.close()
            logger.info("Disconnected from PostgreSQL")
    
    @property
    def pool(self) -> Optional[Pool]:
        """Get the connection pool."""
        return self._pool
    
    async def _create_tables(self):
        """Create required tables if they don't exist."""
        if not self._pool:
            return
        
        async with self._pool.acquire() as conn:
            # Training samples table
            await conn.execute("""
                CREATE TABLE IF NOT EXISTS training_samples (
                    id SERIAL PRIMARY KEY,
                    task_id UUID,
                    task_description TEXT NOT NULL,
                    predicted_type VARCHAR(50) NOT NULL,
                    predicted_complexity VARCHAR(20) NOT NULL,
                    actual_type VARCHAR(50) NOT NULL,
                    actual_complexity VARCHAR(20) NOT NULL,
                    strategy_used VARCHAR(50),
                    success BOOLEAN NOT NULL DEFAULT FALSE,
                    tokens_used INTEGER NOT NULL DEFAULT 0,
                    cost_usd DECIMAL(10,6) NOT NULL DEFAULT 0.0,
                    duration_seconds DECIMAL(10,3) NOT NULL DEFAULT 0.0,
                    error_message TEXT,
                    collected_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    confidence DECIMAL(3,2) NOT NULL DEFAULT 0.5,
                    was_correct BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                )
            """)
            
            # Feedback table for user corrections
            await conn.execute("""
                CREATE TABLE IF NOT EXISTS classification_feedback (
                    id SERIAL PRIMARY KEY,
                    task_description TEXT NOT NULL,
                    predicted_type VARCHAR(50) NOT NULL,
                    predicted_complexity VARCHAR(20) NOT NULL,
                    actual_type VARCHAR(50) NOT NULL,
                    actual_complexity VARCHAR(20) NOT NULL,
                    confidence DECIMAL(3,2) NOT NULL,
                    classifier_used VARCHAR(20) NOT NULL,
                    was_correct BOOLEAN NOT NULL,
                    user_id VARCHAR(100),
                    submitted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                )
            """)
            
            # Model versions table
            await conn.execute("""
                CREATE TABLE IF NOT EXISTS model_versions (
                    id SERIAL PRIMARY KEY,
                    version VARCHAR(50) NOT NULL UNIQUE,
                    model_path VARCHAR(500) NOT NULL,
                    vectorizer_path VARCHAR(500),
                    accuracy DECIMAL(5,4),
                    precision_macro DECIMAL(5,4),
                    recall_macro DECIMAL(5,4),
                    f1_macro DECIMAL(5,4),
                    training_samples INTEGER NOT NULL DEFAULT 0,
                    is_active BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                )
            """)
            
            # Create indexes for performance
            await conn.execute("""
                CREATE INDEX IF NOT EXISTS idx_training_samples_task_id 
                ON training_samples (task_id)
            """)
            
            await conn.execute("""
                CREATE INDEX IF NOT EXISTS idx_training_samples_collected_at 
                ON training_samples (collected_at DESC)
            """)
            
            logger.info("Database tables created/verified")


class TrainingDataRepository:
    """Repository for storing and retrieving training data."""
    
    def __init__(self, db_connection: Optional[DatabaseConnection] = None):
        self.db = db_connection
    
    async def store_feedback(self, feedback_data: dict):
        """Store classification feedback for model training."""
        if not self.db or not self.db.pool:
            logger.warning("No database connection, skipping feedback storage")
            return
        
        try:
            async with self.db.pool.acquire() as conn:
                # Check if this is training sample or user feedback
                if 'task_id' in feedback_data:
                    # Training sample from TaskCompletedEvent
                    await conn.execute("""
                        INSERT INTO training_samples (
                            task_id, task_description, predicted_type, predicted_complexity,
                            actual_type, actual_complexity, strategy_used, success,
                            tokens_used, cost_usd, duration_seconds, error_message,
                            collected_at, confidence, was_correct
                        ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15)
                    """, 
                        feedback_data['task_id'],
                        feedback_data['task_description'],
                        feedback_data['predicted_type'],
                        feedback_data['predicted_complexity'],
                        feedback_data['actual_type'],
                        feedback_data['actual_complexity'],
                        feedback_data['strategy_used'],
                        feedback_data['success'],
                        feedback_data['tokens_used'],
                        feedback_data['cost_usd'],
                        feedback_data['duration_seconds'],
                        feedback_data.get('error_message'),
                        datetime.fromisoformat(feedback_data['collected_at']),
                        feedback_data['confidence'],
                        feedback_data['was_correct']
                    )
                else:
                    # User feedback from API
                    await conn.execute("""
                        INSERT INTO classification_feedback (
                            task_description, predicted_type, predicted_complexity,
                            actual_type, actual_complexity, confidence,
                            classifier_used, was_correct, user_id
                        ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
                    """,
                        feedback_data['task_description'],
                        feedback_data['predicted_type'],
                        feedback_data['predicted_complexity'],
                        feedback_data['actual_type'],
                        feedback_data['actual_complexity'],
                        feedback_data['confidence'],
                        feedback_data['classifier_used'],
                        feedback_data['was_correct'],
                        feedback_data.get('user_id')
                    )
                    
            logger.info("Stored feedback data successfully")
            
        except Exception as e:
            logger.error(f"Error storing feedback data: {e}")
    
    async def get_training_samples(self, limit: int = 1000) -> List[Dict]:
        """Retrieve training samples for model retraining."""
        if not self.db or not self.db.pool:
            logger.warning("No database connection, returning empty training samples")
            return []
        
        try:
            async with self.db.pool.acquire() as conn:
                # Get both training samples and user feedback
                training_query = """
                    SELECT 
                        task_description,
                        actual_type as task_type,
                        actual_complexity as complexity,
                        success,
                        tokens_used,
                        duration_seconds,
                        collected_at as timestamp
                    FROM training_samples 
                    WHERE task_description IS NOT NULL 
                    AND LENGTH(task_description) > 10
                    ORDER BY collected_at DESC 
                    LIMIT $1
                """
                
                feedback_query = """
                    SELECT 
                        task_description,
                        actual_type as task_type,
                        actual_complexity as complexity,
                        TRUE as success,
                        0 as tokens_used,
                        0.0 as duration_seconds,
                        submitted_at as timestamp
                    FROM classification_feedback 
                    WHERE task_description IS NOT NULL 
                    AND LENGTH(task_description) > 10
                    ORDER BY submitted_at DESC 
                    LIMIT $1
                """
                
                # Execute both queries
                training_rows = await conn.fetch(training_query, limit // 2)
                feedback_rows = await conn.fetch(feedback_query, limit // 2)
                
                # Convert to dictionaries
                samples = []
                
                for row in training_rows:
                    samples.append({
                        'task_description': row['task_description'],
                        'task_type': row['task_type'],
                        'complexity': row['complexity'],
                        'success': row['success'],
                        'tokens_used': row['tokens_used'],
                        'duration_seconds': float(row['duration_seconds']),
                        'timestamp': row['timestamp'].isoformat()
                    })
                
                for row in feedback_rows:
                    samples.append({
                        'task_description': row['task_description'],
                        'task_type': row['task_type'],
                        'complexity': row['complexity'],
                        'success': row['success'],
                        'tokens_used': row['tokens_used'],
                        'duration_seconds': float(row['duration_seconds']),
                        'timestamp': row['timestamp'].isoformat()
                    })
                
                logger.info(f"Retrieved {len(samples)} training samples from database")
                return samples
                
        except Exception as e:
            logger.error(f"Error retrieving training samples: {e}")
            return []
    
    async def get_sample_count(self) -> int:
        """Get total count of training samples."""
        if not self.db or not self.db.pool:
            return 0
        
        try:
            async with self.db.pool.acquire() as conn:
                training_count = await conn.fetchval(
                    "SELECT COUNT(*) FROM training_samples"
                )
                feedback_count = await conn.fetchval(
                    "SELECT COUNT(*) FROM classification_feedback"
                )
                return (training_count or 0) + (feedback_count or 0)
        except Exception as e:
            logger.error(f"Error getting sample count: {e}")
            return 0
    
    async def save_model_version(self, version_data: Dict):
        """Save model version metadata."""
        if not self.db or not self.db.pool:
            logger.warning("No database connection, skipping model version save")
            return
        
        try:
            async with self.db.pool.acquire() as conn:
                await conn.execute("""
                    INSERT INTO model_versions (
                        version, model_path, vectorizer_path, accuracy,
                        precision_macro, recall_macro, f1_macro,
                        training_samples, is_active
                    ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
                """,
                    version_data['version'],
                    version_data['model_path'],
                    version_data.get('vectorizer_path'),
                    version_data.get('accuracy'),
                    version_data.get('precision_macro'),
                    version_data.get('recall_macro'),
                    version_data.get('f1_macro'),
                    version_data.get('training_samples', 0),
                    version_data.get('is_active', False)
                )
                
                # If this is the new active model, deactivate others
                if version_data.get('is_active', False):
                    await conn.execute("""
                        UPDATE model_versions 
                        SET is_active = FALSE 
                        WHERE version != $1
                    """, version_data['version'])
                    
            logger.info(f"Saved model version: {version_data['version']}")
            
        except Exception as e:
            logger.error(f"Error saving model version: {e}")


# Module-level connection instance
_db_connection: Optional[DatabaseConnection] = None


async def init_db(connection_string: Optional[str] = None):
    """Initialize database connection."""
    global _db_connection
    _db_connection = DatabaseConnection(connection_string)
    await _db_connection.connect()
    logger.info("Database connection initialized")


async def close_db():
    """Close database connection."""
    global _db_connection
    if _db_connection:
        await _db_connection.disconnect()
        _db_connection = None
    logger.info("Database connection closed")


def get_training_repo() -> TrainingDataRepository:
    """Get training data repository instance."""
    return TrainingDataRepository(_db_connection)
