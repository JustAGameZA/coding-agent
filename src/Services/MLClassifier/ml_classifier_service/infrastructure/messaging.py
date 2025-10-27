"""RabbitMQ messaging infrastructure for event-driven communication.

Implements event consumer for TaskCompletedEvent to collect training data
for ML model improvement.
"""

import asyncio
import json
import logging
from datetime import datetime
from typing import Callable, Dict, Optional

import pika
import pika.adapters.asyncio_connection
from pika.channel import Channel
from pika.connection import Connection

logger = logging.getLogger(__name__)


class EventConsumer:
    """RabbitMQ consumer for processing domain events."""
    
    def __init__(self, rabbitmq_url: Optional[str] = None):
        self.rabbitmq_url = rabbitmq_url or "amqp://codingagent:devPassword123!@localhost:5672/"
        self._connection: Optional[Connection] = None
        self._channel: Optional[Channel] = None
        self._handlers: Dict[str, Callable] = {}
        self._consuming = False
    
    async def connect(self):
        """Initialize RabbitMQ connection."""
        try:
            logger.info(f"Connecting to RabbitMQ at {self.rabbitmq_url}")
            
            # Parse connection parameters
            parameters = pika.URLParameters(self.rabbitmq_url)
            
            # Create connection
            self._connection = await asyncio.get_event_loop().run_in_executor(
                None, 
                lambda: pika.BlockingConnection(parameters)
            )
            self._channel = self._connection.channel()
            
            # Declare exchange and queue for ML Classifier events
            self._channel.exchange_declare(
                exchange='coding_agent_events',
                exchange_type='topic',
                durable=True
            )
            
            # Declare queue for ML Classifier service
            queue_result = self._channel.queue_declare(
                queue='ml_classifier_training',
                durable=True
            )
            queue_name = queue_result.method.queue
            
            # Bind queue to receive TaskCompletedEvent
            self._channel.queue_bind(
                exchange='coding_agent_events',
                queue=queue_name,
                routing_key='task.completed'
            )
            
            logger.info(f"Connected to RabbitMQ, listening on queue: {queue_name}")
            
        except Exception as e:
            logger.error(f"Failed to connect to RabbitMQ: {e}")
            logger.warning("ML Classifier will continue without event consumption")
            # Don't raise - allow service to start without RabbitMQ
    
    async def disconnect(self):
        """Close RabbitMQ connection."""
        if self._consuming:
            await self.stop_consuming()
        
        if self._connection and not self._connection.is_closed:
            self._connection.close()
            logger.info("Disconnected from RabbitMQ")
    
    def register_handler(self, event_type: str, handler: Callable):
        """Register an event handler for a specific event type."""
        logger.info(f"Registering handler for event type: {event_type}")
        self._handlers[event_type] = handler
    
    async def start_consuming(self):
        """Start consuming messages from RabbitMQ."""
        if not self._channel:
            logger.warning("No RabbitMQ channel available, skipping event consumption")
            return
        
        try:
            # Set up message consumption
            self._channel.basic_consume(
                queue='ml_classifier_training',
                on_message_callback=self._on_message,
                auto_ack=False  # Manual acknowledgment for reliability
            )
            
            self._consuming = True
            logger.info("Started consuming RabbitMQ messages")
            
            # Start consuming in background thread
            def consume_loop():
                try:
                    self._channel.start_consuming()
                except Exception as e:
                    logger.error(f"Error in consume loop: {e}")
                    self._consuming = False
            
            await asyncio.get_event_loop().run_in_executor(None, consume_loop)
            
        except Exception as e:
            logger.error(f"Failed to start consuming: {e}")
            self._consuming = False
    
    async def stop_consuming(self):
        """Stop consuming messages."""
        if self._consuming and self._channel:
            self._channel.stop_consuming()
            self._consuming = False
            logger.info("Stopped consuming RabbitMQ messages")
    
    def _on_message(self, channel, method, properties, body):
        """Handle incoming RabbitMQ message."""
        try:
            # Parse message
            message_data = json.loads(body.decode('utf-8'))
            event_type = message_data.get('EventType', 'Unknown')
            
            logger.info(f"Received event: {event_type}")
            
            # Route to appropriate handler
            if event_type in self._handlers:
                # Run handler in async context
                asyncio.create_task(self._handlers[event_type](message_data))
            else:
                logger.warning(f"No handler registered for event type: {event_type}")
            
            # Acknowledge message
            channel.basic_ack(delivery_tag=method.delivery_tag)
            
        except Exception as e:
            logger.error(f"Error processing message: {e}")
            # Reject message and don't requeue on error
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=False)


class EventPublisher:
    """RabbitMQ publisher for sending domain events."""
    
    def __init__(self, rabbitmq_url: Optional[str] = None):
        self.rabbitmq_url = rabbitmq_url
        self._connection = None
        self._channel = None
    
    async def connect(self):
        """Initialize RabbitMQ connection."""
        logger.info("RabbitMQ publisher connection will be implemented in Phase 2")
        # Future: Initialize RabbitMQ connection for publishing
    
    async def disconnect(self):
        """Close RabbitMQ connection."""
        logger.info("RabbitMQ publisher disconnection will be implemented in Phase 2")
        # Future: Close connection
    
    async def publish(self, event_type: str, event_data: dict):
        """Publish an event to RabbitMQ."""
        logger.info(f"Publishing event: {event_type} (Phase 2 placeholder)")
        # Future: Publish to exchange
        # await self._channel.basic_publish(
        #     exchange='domain_events',
        #     routing_key=event_type,
        #     body=json.dumps(event_data)
        # )


# Module-level instances
_consumer: Optional[EventConsumer] = None
_publisher: Optional[EventPublisher] = None


async def start_consumer(rabbitmq_url: Optional[str] = None):
    """Initialize and start event consumer."""
    global _consumer
    _consumer = EventConsumer(rabbitmq_url)
    await _consumer.connect()
    
    # Register event handlers for training data collection
    _consumer.register_handler("TaskCompletedEvent", handle_task_completed)
    
    # Start consuming in background task
    asyncio.create_task(_consumer.start_consuming())
    logger.info("Event consumer started and listening for TaskCompletedEvent")


async def stop_consumer():
    """Stop and disconnect event consumer."""
    global _consumer
    if _consumer:
        await _consumer.stop_consuming()
        await _consumer.disconnect()
    logger.info("Event consumer stopped (Phase 2 placeholder)")


def get_publisher() -> EventPublisher:
    """Get event publisher instance."""
    if _publisher is None:
        return EventPublisher()
    return _publisher


# Example event handlers for future implementation
async def handle_task_completed(event_data: dict):
    """Handle TaskCompletedEvent for collecting training data.
    
    Event structure from .NET SharedKernel:
    {
        "EventId": "guid",
        "OccurredAt": "datetime",
        "TaskId": "guid",
        "TaskDescription": "string",
        "TaskType": "BugFix|Feature|Refactor|Test|Documentation|Deployment",
        "Complexity": "Simple|Medium|Complex",
        "Strategy": "SingleShot|Iterative|MultiAgent|HybridExecution",
        "Success": bool,
        "TokensUsed": int,
        "CostUsd": decimal,
        "Duration": "timespan",
        "ErrorMessage": string? (optional)
    }
    """
    try:
        task_id = event_data.get('TaskId')
        logger.info(f"Processing TaskCompletedEvent for task {task_id}")
        
        from infrastructure.database import get_training_repo
        repo = get_training_repo()
        
        # Parse duration (format: "00:01:23.456" -> seconds)
        duration_str = event_data.get('Duration', '00:00:00')
        duration_seconds = _parse_timespan_to_seconds(duration_str)
        
        # Extract training sample from completed task
        training_sample = {
            "task_id": task_id,
            "task_description": event_data.get('TaskDescription', ''),
            "predicted_type": event_data.get('TaskType', 'Feature'),  # What classifier predicted
            "predicted_complexity": event_data.get('Complexity', 'Medium'),  # What classifier predicted
            "actual_type": event_data.get('TaskType', 'Feature'),  # Ground truth from execution
            "actual_complexity": _calculate_actual_complexity(event_data),  # Calculate from execution data
            "strategy_used": event_data.get('Strategy', 'Iterative'),
            "success": event_data.get('Success', False),
            "tokens_used": int(event_data.get('TokensUsed', 0)),
            "cost_usd": float(event_data.get('CostUsd', 0.0)),
            "duration_seconds": duration_seconds,
            "error_message": event_data.get('ErrorMessage'),
            "collected_at": datetime.utcnow().isoformat(),
            "confidence": 0.85,  # Default confidence for training data
            "was_correct": True  # Assume correct for ground truth data
        }
        
        # Store training sample
        await repo.store_feedback(training_sample)
        logger.info(f"Stored training sample for task {task_id}: {training_sample['actual_type']}/{training_sample['actual_complexity']}")
        
        # Check if we should trigger retraining
        sample_count = await repo.get_sample_count()
        if sample_count > 0 and sample_count % 1000 == 0:
            logger.info(f"Reached {sample_count} samples, considering model retraining")
            # Could trigger async retraining here
            
    except Exception as e:
        logger.error(f"Error handling TaskCompletedEvent: {e}")


def _parse_timespan_to_seconds(timespan_str: str) -> float:
    """Parse .NET TimeSpan string to seconds.
    
    Formats: "00:01:23.456" or "1.02:03:04.567"
    """
    try:
        parts = timespan_str.split(':')
        if len(parts) == 3:
            # Format: "HH:MM:SS.fff"
            hours = int(parts[0])
            minutes = int(parts[1])
            seconds_parts = parts[2].split('.')
            seconds = int(seconds_parts[0])
            milliseconds = int(seconds_parts[1][:3]) if len(seconds_parts) > 1 else 0
            
            return hours * 3600 + minutes * 60 + seconds + milliseconds / 1000.0
        else:
            logger.warning(f"Unexpected timespan format: {timespan_str}")
            return 0.0
    except Exception as e:
        logger.error(f"Error parsing timespan '{timespan_str}': {e}")
        return 0.0


def _calculate_actual_complexity(event_data: dict) -> str:
    """Calculate actual task complexity from execution metrics.
    
    Uses tokens used and duration to infer actual complexity.
    """
    tokens_used = int(event_data.get('TokensUsed', 0))
    duration_str = event_data.get('Duration', '00:00:00')
    duration_seconds = _parse_timespan_to_seconds(duration_str)
    success = event_data.get('Success', False)
    
    # Complexity heuristics based on execution data
    if tokens_used < 2000 and duration_seconds < 30:
        return 'Simple'
    elif tokens_used > 10000 or duration_seconds > 300 or not success:
        return 'Complex'
    else:
        return 'Medium'
