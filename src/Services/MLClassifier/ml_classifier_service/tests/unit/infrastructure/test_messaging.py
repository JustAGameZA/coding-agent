"""Unit tests for RabbitMQ messaging infrastructure."""

import json
import pytest
from unittest.mock import AsyncMock, MagicMock, patch

from infrastructure.messaging import EventConsumer, handle_task_completed, _parse_timespan_to_seconds, _calculate_actual_complexity


class TestEventConsumer:
    """Test EventConsumer class."""

    def test_init(self):
        """Test EventConsumer initialization."""
        consumer = EventConsumer()
        assert consumer.rabbitmq_url == "amqp://codingagent:devPassword123!@localhost:5672/"
        assert consumer._connection is None
        assert consumer._channel is None
        assert consumer._handlers == {}
        assert consumer._consuming is False

    def test_init_with_custom_url(self):
        """Test EventConsumer initialization with custom URL."""
        custom_url = "amqp://user:pass@host:5672/"
        consumer = EventConsumer(custom_url)
        assert consumer.rabbitmq_url == custom_url

    def test_register_handler(self):
        """Test handler registration."""
        consumer = EventConsumer()
        handler = AsyncMock()
        
        consumer.register_handler("TestEvent", handler)
        assert "TestEvent" in consumer._handlers
        assert consumer._handlers["TestEvent"] == handler

    @pytest.mark.asyncio
    async def test_connect_failure(self):
        """Test connection failure handling."""
        consumer = EventConsumer("amqp://invalid:invalid@nonexistent:5672/")
        
        # Should not raise exception on connection failure
        await consumer.connect()
        assert consumer._connection is None

    @pytest.mark.asyncio
    async def test_disconnect_without_connection(self):
        """Test disconnect without active connection."""
        consumer = EventConsumer()
        
        # Should not raise exception
        await consumer.disconnect()

    @pytest.mark.asyncio
    async def test_start_consuming_without_channel(self):
        """Test consuming without channel."""
        consumer = EventConsumer()
        
        # Should not raise exception
        await consumer.start_consuming()
        assert not consumer._consuming


class TestMessageHandlers:
    """Test message handling functions."""

    @pytest.mark.asyncio
    async def test_handle_task_completed_success(self):
        """Test successful TaskCompletedEvent handling."""
        event_data = {
            "TaskId": "12345678-1234-1234-1234-123456789012",
            "TaskDescription": "Fix login bug in authentication service",
            "TaskType": "BugFix",
            "Complexity": "Simple",
            "Strategy": "SingleShot",
            "Success": True,
            "TokensUsed": 1500,
            "CostUsd": 0.003,
            "Duration": "00:01:23.456",
            "ErrorMessage": None
        }

        with patch('infrastructure.messaging.get_training_repo') as mock_repo_factory:
            mock_repo = AsyncMock()
            mock_repo.store_feedback = AsyncMock()
            mock_repo.get_sample_count = AsyncMock(return_value=999)
            mock_repo_factory.return_value = mock_repo

            await handle_task_completed(event_data)

            # Verify feedback was stored
            mock_repo.store_feedback.assert_called_once()
            stored_data = mock_repo.store_feedback.call_args[0][0]
            
            assert stored_data["task_id"] == event_data["TaskId"]
            assert stored_data["task_description"] == event_data["TaskDescription"]
            assert stored_data["predicted_type"] == "BugFix"
            assert stored_data["actual_type"] == "BugFix"
            assert stored_data["success"] is True
            assert stored_data["tokens_used"] == 1500
            assert stored_data["duration_seconds"] == 83.456  # 1 minute 23.456 seconds

    @pytest.mark.asyncio
    async def test_handle_task_completed_retraining_trigger(self):
        """Test retraining trigger at 1000 samples."""
        event_data = {
            "TaskId": "12345678-1234-1234-1234-123456789012",
            "TaskDescription": "Test task",
            "TaskType": "Feature",
            "Complexity": "Medium",
            "Strategy": "Iterative",
            "Success": True,
            "TokensUsed": 5000,
            "CostUsd": 0.01,
            "Duration": "00:05:30.000"
        }

        with patch('infrastructure.messaging.get_training_repo') as mock_repo_factory:
            mock_repo = AsyncMock()
            mock_repo.store_feedback = AsyncMock()
            mock_repo.get_sample_count = AsyncMock(return_value=1000)  # Trigger retraining
            mock_repo_factory.return_value = mock_repo

            await handle_task_completed(event_data)

            mock_repo.get_sample_count.assert_called_once()

    @pytest.mark.asyncio
    async def test_handle_task_completed_error_handling(self):
        """Test error handling in TaskCompletedEvent processing."""
        event_data = {"invalid": "data"}

        with patch('infrastructure.messaging.get_training_repo') as mock_repo_factory:
            mock_repo_factory.side_effect = Exception("Database error")

            # Should not raise exception
            await handle_task_completed(event_data)


class TestUtilityFunctions:
    """Test utility functions."""

    def test_parse_timespan_to_seconds_basic(self):
        """Test basic timespan parsing."""
        assert _parse_timespan_to_seconds("00:01:23.456") == 83.456
        assert _parse_timespan_to_seconds("01:30:45.000") == 5445.0
        assert _parse_timespan_to_seconds("00:00:05.123") == 5.123

    def test_parse_timespan_to_seconds_no_milliseconds(self):
        """Test timespan parsing without milliseconds."""
        assert _parse_timespan_to_seconds("00:01:23") == 83.0

    def test_parse_timespan_to_seconds_invalid_format(self):
        """Test timespan parsing with invalid format."""
        assert _parse_timespan_to_seconds("invalid") == 0.0
        assert _parse_timespan_to_seconds("") == 0.0
        assert _parse_timespan_to_seconds("1:2") == 0.0

    def test_calculate_actual_complexity_simple(self):
        """Test complexity calculation for simple tasks."""
        event_data = {
            "TokensUsed": 1000,
            "Duration": "00:00:15.000",
            "Success": True
        }
        assert _calculate_actual_complexity(event_data) == "Simple"

    def test_calculate_actual_complexity_medium(self):
        """Test complexity calculation for medium tasks."""
        event_data = {
            "TokensUsed": 5000,
            "Duration": "00:02:30.000",
            "Success": True
        }
        assert _calculate_actual_complexity(event_data) == "Medium"

    def test_calculate_actual_complexity_complex_by_tokens(self):
        """Test complexity calculation for complex tasks (high tokens)."""
        event_data = {
            "TokensUsed": 15000,
            "Duration": "00:01:00.000",
            "Success": True
        }
        assert _calculate_actual_complexity(event_data) == "Complex"

    def test_calculate_actual_complexity_complex_by_duration(self):
        """Test complexity calculation for complex tasks (long duration)."""
        event_data = {
            "TokensUsed": 3000,
            "Duration": "00:06:00.000",
            "Success": True
        }
        assert _calculate_actual_complexity(event_data) == "Complex"

    def test_calculate_actual_complexity_complex_by_failure(self):
        """Test complexity calculation for complex tasks (failed)."""
        event_data = {
            "TokensUsed": 2000,
            "Duration": "00:01:00.000",
            "Success": False
        }
        assert _calculate_actual_complexity(event_data) == "Complex"