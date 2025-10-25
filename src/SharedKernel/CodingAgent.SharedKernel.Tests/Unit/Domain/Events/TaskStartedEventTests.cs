using CodingAgent.SharedKernel.Domain.Events;
using CodingAgent.SharedKernel.Domain.ValueObjects;
using FluentAssertions;

namespace CodingAgent.SharedKernel.Tests.Unit.Domain.Events;

[Trait("Category", "Unit")]
public class TaskStartedEventTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var @event = new TaskStartedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.BugFix,
            Complexity = TaskComplexity.Simple,
            Strategy = ExecutionStrategy.SingleShot,
            UserId = Guid.NewGuid()
        };

        // Assert
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.TaskId.Should().NotBeEmpty();
        @event.TaskType.Should().Be(TaskType.BugFix);
        @event.Complexity.Should().Be(TaskComplexity.Simple);
        @event.Strategy.Should().Be(ExecutionStrategy.SingleShot);
        @event.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public void EventId_ShouldBeUnique()
    {
        // Arrange & Act
        var event1 = new TaskStartedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Feature,
            Complexity = TaskComplexity.Medium,
            Strategy = ExecutionStrategy.Iterative,
            UserId = Guid.NewGuid()
        };

        var event2 = new TaskStartedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Feature,
            Complexity = TaskComplexity.Medium,
            Strategy = ExecutionStrategy.Iterative,
            UserId = Guid.NewGuid()
        };

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void Record_ShouldSupportValueEquality()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var event1 = new TaskStartedEvent
        {
            TaskId = taskId,
            TaskType = TaskType.Refactor,
            Complexity = TaskComplexity.Complex,
            Strategy = ExecutionStrategy.MultiAgent,
            UserId = userId
        }
        with
        {
            EventId = eventId,
            OccurredAt = occurredAt
        };

        var event2 = new TaskStartedEvent
        {
            TaskId = taskId,
            TaskType = TaskType.Refactor,
            Complexity = TaskComplexity.Complex,
            Strategy = ExecutionStrategy.MultiAgent,
            UserId = userId
        }
        with
        {
            EventId = eventId,
            OccurredAt = occurredAt
        };

        // Assert
        event1.Should().Be(event2);
    }

    [Theory]
    [InlineData(TaskType.BugFix)]
    [InlineData(TaskType.Feature)]
    [InlineData(TaskType.Refactor)]
    [InlineData(TaskType.Documentation)]
    [InlineData(TaskType.Test)]
    [InlineData(TaskType.Deployment)]
    public void TaskType_ShouldAcceptAllValidValues(TaskType taskType)
    {
        // Arrange & Act
        var @event = new TaskStartedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = taskType,
            Complexity = TaskComplexity.Simple,
            Strategy = ExecutionStrategy.SingleShot,
            UserId = Guid.NewGuid()
        };

        // Assert
        @event.TaskType.Should().Be(taskType);
    }

    [Theory]
    [InlineData(TaskComplexity.Simple)]
    [InlineData(TaskComplexity.Medium)]
    [InlineData(TaskComplexity.Complex)]
    [InlineData(TaskComplexity.Epic)]
    public void Complexity_ShouldAcceptAllValidValues(TaskComplexity complexity)
    {
        // Arrange & Act
        var @event = new TaskStartedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Feature,
            Complexity = complexity,
            Strategy = ExecutionStrategy.Iterative,
            UserId = Guid.NewGuid()
        };

        // Assert
        @event.Complexity.Should().Be(complexity);
    }

    [Theory]
    [InlineData(ExecutionStrategy.SingleShot)]
    [InlineData(ExecutionStrategy.Iterative)]
    [InlineData(ExecutionStrategy.MultiAgent)]
    [InlineData(ExecutionStrategy.HybridExecution)]
    public void Strategy_ShouldAcceptAllValidValues(ExecutionStrategy strategy)
    {
        // Arrange & Act
        var @event = new TaskStartedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Feature,
            Complexity = TaskComplexity.Medium,
            Strategy = strategy,
            UserId = Guid.NewGuid()
        };

        // Assert
        @event.Strategy.Should().Be(strategy);
    }
}
