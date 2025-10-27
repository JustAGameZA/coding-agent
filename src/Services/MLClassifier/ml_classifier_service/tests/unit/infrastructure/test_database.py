"""Unit tests for database infrastructure."""

import pytest
from unittest.mock import AsyncMock, MagicMock, patch
from datetime import datetime

from infrastructure.database import DatabaseConnection, TrainingDataRepository


class TestDatabaseConnection:
    """Test DatabaseConnection class."""

    def test_init_default_connection(self):
        """Test DatabaseConnection initialization with default connection string."""
        db = DatabaseConnection()
        assert "postgresql://postgres:devPassword123!@localhost:5432/coding_agent_ml" in db.connection_string
        assert db._pool is None

    def test_init_custom_connection(self):
        """Test DatabaseConnection initialization with custom connection string."""
        custom_conn = "postgresql://user:pass@host:5432/db"
        db = DatabaseConnection(custom_conn)
        assert db.connection_string == custom_conn

    @pytest.mark.asyncio
    async def test_connect_failure(self):
        """Test connection failure handling."""
        db = DatabaseConnection("postgresql://invalid:invalid@nonexistent:5432/invalid")
        
        # Should not raise exception on connection failure
        await db.connect()
        assert db.pool is None

    @pytest.mark.asyncio
    async def test_disconnect_without_connection(self):
        """Test disconnect without active connection."""
        db = DatabaseConnection()
        
        # Should not raise exception
        await db.disconnect()

    def test_pool_property(self):
        """Test pool property."""
        db = DatabaseConnection()
        assert db.pool is None


class TestTrainingDataRepository:
    """Test TrainingDataRepository class."""

    def test_init(self):
        """Test repository initialization."""
        db_conn = MagicMock()
        repo = TrainingDataRepository(db_conn)
        assert repo.db == db_conn

    def test_init_without_connection(self):
        """Test repository initialization without connection."""
        repo = TrainingDataRepository()
        assert repo.db is None

    @pytest.mark.asyncio
    async def test_store_feedback_without_connection(self):
        """Test storing feedback without database connection."""
        repo = TrainingDataRepository()
        
        # Should not raise exception
        await repo.store_feedback({"test": "data"})

    @pytest.mark.asyncio
    async def test_store_feedback_training_sample(self):
        """Test storing training sample feedback."""
        # Mock database connection
        mock_conn = AsyncMock()
        mock_pool = AsyncMock()
        mock_pool.acquire.return_value.__aenter__.return_value = mock_conn
        
        mock_db = MagicMock()
        mock_db.pool = mock_pool
        
        repo = TrainingDataRepository(mock_db)
        
        # Training sample data (has task_id)
        feedback_data = {
            "task_id": "12345678-1234-1234-1234-123456789012",
            "task_description": "Fix login bug",
            "predicted_type": "BugFix",
            "predicted_complexity": "Simple",
            "actual_type": "BugFix",
            "actual_complexity": "Simple",
            "strategy_used": "SingleShot",
            "success": True,
            "tokens_used": 1500,
            "cost_usd": 0.003,
            "duration_seconds": 83.456,
            "error_message": None,
            "collected_at": "2025-10-27T10:30:00",
            "confidence": 0.85,
            "was_correct": True
        }
        
        await repo.store_feedback(feedback_data)
        
        # Verify INSERT into training_samples was called
        mock_conn.execute.assert_called_once()
        call_args = mock_conn.execute.call_args
        assert "INSERT INTO training_samples" in call_args[0][0]

    @pytest.mark.asyncio
    async def test_store_feedback_user_feedback(self):
        """Test storing user feedback."""
        # Mock database connection
        mock_conn = AsyncMock()
        mock_pool = AsyncMock()
        mock_pool.acquire.return_value.__aenter__.return_value = mock_conn
        
        mock_db = MagicMock()
        mock_db.pool = mock_pool
        
        repo = TrainingDataRepository(mock_db)
        
        # User feedback data (no task_id)
        feedback_data = {
            "task_description": "Add new feature",
            "predicted_type": "BugFix",
            "predicted_complexity": "Simple",
            "actual_type": "Feature",
            "actual_complexity": "Medium",
            "confidence": 0.75,
            "classifier_used": "heuristic",
            "was_correct": False,
            "user_id": "user123"
        }
        
        await repo.store_feedback(feedback_data)
        
        # Verify INSERT into classification_feedback was called
        mock_conn.execute.assert_called_once()
        call_args = mock_conn.execute.call_args
        assert "INSERT INTO classification_feedback" in call_args[0][0]

    @pytest.mark.asyncio
    async def test_store_feedback_error_handling(self):
        """Test error handling in store_feedback."""
        mock_conn = AsyncMock()
        mock_conn.execute.side_effect = Exception("Database error")
        mock_pool = AsyncMock()
        mock_pool.acquire.return_value.__aenter__.return_value = mock_conn
        
        mock_db = MagicMock()
        mock_db.pool = mock_pool
        
        repo = TrainingDataRepository(mock_db)
        
        # Should not raise exception
        await repo.store_feedback({"test": "data"})

    @pytest.mark.asyncio
    async def test_get_training_samples_without_connection(self):
        """Test getting training samples without database connection."""
        repo = TrainingDataRepository()
        
        samples = await repo.get_training_samples(100)
        assert samples == []

    @pytest.mark.asyncio
    async def test_get_training_samples_success(self):
        """Test successful training samples retrieval."""
        # Mock database responses
        training_rows = [
            {
                "task_description": "Fix bug",
                "task_type": "BugFix",
                "complexity": "Simple",
                "success": True,
                "tokens_used": 1500,
                "duration_seconds": 83.456,
                "timestamp": datetime(2025, 10, 27, 10, 30, 0)
            }
        ]
        
        feedback_rows = [
            {
                "task_description": "Add feature", 
                "task_type": "Feature",
                "complexity": "Medium",
                "success": True,
                "tokens_used": 0,
                "duration_seconds": 0.0,
                "timestamp": datetime(2025, 10, 27, 11, 0, 0)
            }
        ]
        
        mock_conn = AsyncMock()
        mock_conn.fetch.side_effect = [training_rows, feedback_rows]
        mock_pool = AsyncMock()
        mock_pool.acquire.return_value.__aenter__.return_value = mock_conn
        
        mock_db = MagicMock()
        mock_db.pool = mock_pool
        
        repo = TrainingDataRepository(mock_db)
        
        samples = await repo.get_training_samples(100)
        
        assert len(samples) == 2
        assert samples[0]["task_description"] == "Fix bug"
        assert samples[0]["task_type"] == "BugFix"
        assert samples[1]["task_description"] == "Add feature"
        assert samples[1]["task_type"] == "Feature"

    @pytest.mark.asyncio
    async def test_get_sample_count_without_connection(self):
        """Test getting sample count without database connection."""
        repo = TrainingDataRepository()
        
        count = await repo.get_sample_count()
        assert count == 0

    @pytest.mark.asyncio
    async def test_get_sample_count_success(self):
        """Test successful sample count retrieval."""
        mock_conn = AsyncMock()
        mock_conn.fetchval.side_effect = [500, 250]  # training + feedback counts
        mock_pool = AsyncMock()
        mock_pool.acquire.return_value.__aenter__.return_value = mock_conn
        
        mock_db = MagicMock()
        mock_db.pool = mock_pool
        
        repo = TrainingDataRepository(mock_db)
        
        count = await repo.get_sample_count()
        assert count == 750

    @pytest.mark.asyncio
    async def test_save_model_version_without_connection(self):
        """Test saving model version without database connection."""
        repo = TrainingDataRepository()
        
        # Should not raise exception
        await repo.save_model_version({"version": "v1.0.0"})

    @pytest.mark.asyncio
    async def test_save_model_version_success(self):
        """Test successful model version saving."""
        mock_conn = AsyncMock()
        mock_pool = AsyncMock()
        mock_pool.acquire.return_value.__aenter__.return_value = mock_conn
        
        mock_db = MagicMock()
        mock_db.pool = mock_pool
        
        repo = TrainingDataRepository(mock_db)
        
        version_data = {
            "version": "v1.0.0",
            "model_path": "/models/v1.0.0.json",
            "vectorizer_path": "/models/v1.0.0_vectorizer.pkl",
            "accuracy": 0.92,
            "precision_macro": 0.90,
            "recall_macro": 0.89,
            "f1_macro": 0.88,
            "training_samples": 1000,
            "is_active": True
        }
        
        await repo.save_model_version(version_data)
        
        # Verify INSERT and UPDATE were called
        assert mock_conn.execute.call_count == 2
        
        # Check INSERT call
        insert_call = mock_conn.execute.call_args_list[0]
        assert "INSERT INTO model_versions" in insert_call[0][0]
        
        # Check UPDATE call (deactivate other models)
        update_call = mock_conn.execute.call_args_list[1]
        assert "UPDATE model_versions" in update_call[0][0]
        assert "SET is_active = FALSE" in update_call[0][0]

    @pytest.mark.asyncio
    async def test_save_model_version_error_handling(self):
        """Test error handling in save_model_version."""
        mock_conn = AsyncMock()
        mock_conn.execute.side_effect = Exception("Database error")
        mock_pool = AsyncMock()
        mock_pool.acquire.return_value.__aenter__.return_value = mock_conn
        
        mock_db = MagicMock()
        mock_db.pool = mock_pool
        
        repo = TrainingDataRepository(mock_db)
        
        # Should not raise exception
        await repo.save_model_version({"version": "v1.0.0"})