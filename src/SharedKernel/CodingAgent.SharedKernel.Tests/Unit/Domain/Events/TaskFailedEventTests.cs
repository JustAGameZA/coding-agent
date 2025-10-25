using CodingAgent.SharedKernel.Domain.Events;
using CodingAgent.SharedKernel.Domain.ValueObjects;
using FluentAssertions;

namespace CodingAgent.SharedKernel.Tests.Unit.Domain.Events;

[Trait("Category", "Unit")]
public class TaskFailedEventTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var @event = new TaskFailedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.BugFix,
            Complexity = TaskComplexity.Simple,
            Strategy = ExecutionStrategy.SingleShot,
            ErrorMessage = "Task execution timed out",
            TokensUsed = 1500,
            CostUsd = 0.05m,
            Duration = TimeSpan.FromMinutes(5)
        };

        // Assert
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.TaskId.Should().NotBeEmpty();
        @event.TaskType.Should().Be(TaskType.BugFix);
        @event.Complexity.Should().Be(TaskComplexity.Simple);
        @event.Strategy.Should().Be(ExecutionStrategy.SingleShot);
        @event.ErrorMessage.Should().Be("Task execution timed out");
        @event.TokensUsed.Should().Be(1500);
        @event.CostUsd.Should().Be(0.05m);
        @event.Duration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void EventId_ShouldBeUnique()
    {
        // Arrange & Act
        var event1 = new TaskFailedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Feature,
            Complexity = TaskComplexity.Medium,
            Strategy = ExecutionStrategy.Iterative,
            ErrorMessage = "Error 1",
            Duration = TimeSpan.FromMinutes(1)
        };

        var event2 = new TaskFailedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Feature,
            Complexity = TaskComplexity.Medium,
            Strategy = ExecutionStrategy.Iterative,
            ErrorMessage = "Error 2",
            Duration = TimeSpan.FromMinutes(1)
        };

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void Record_ShouldSupportValueEquality()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var event1 = new TaskFailedEvent
        {
            TaskId = taskId,
            TaskType = TaskType.Refactor,
            Complexity = TaskComplexity.Complex,
            Strategy = ExecutionStrategy.MultiAgent,
            ErrorMessage = "Execution failed",
            TokensUsed = 5000,
            CostUsd = 0.25m,
            Duration = TimeSpan.FromMinutes(10)
        }
        with
        {
            EventId = eventId,
            OccurredAt = occurredAt
        };

        var event2 = new TaskFailedEvent
        {
            TaskId = taskId,
            TaskType = TaskType.Refactor,
            Complexity = TaskComplexity.Complex,
            Strategy = ExecutionStrategy.MultiAgent,
            ErrorMessage = "Execution failed",
            TokensUsed = 5000,
            CostUsd = 0.25m,
            Duration = TimeSpan.FromMinutes(10)
        }
        with
        {
            EventId = eventId,
            OccurredAt = occurredAt
        };

        // Assert
        event1.Should().Be(event2);
    }

    [Fact]
    public void ErrorMessage_ShouldBeRequired()
    {
        // Arrange & Act
        var @event = new TaskFailedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Test,
            Complexity = TaskComplexity.Simple,
            Strategy = ExecutionStrategy.SingleShot,
            ErrorMessage = "Detailed error message",
            Duration = TimeSpan.FromSeconds(30)
        };

        // Assert
        @event.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TokensUsed_ShouldDefaultToZero()
    {
        // Arrange & Act
        var @event = new TaskFailedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Documentation,
            Complexity = TaskComplexity.Simple,
            Strategy = ExecutionStrategy.SingleShot,
            ErrorMessage = "Failed to parse documentation",
            Duration = TimeSpan.FromMinutes(2)
        };

        // Assert
        @event.TokensUsed.Should().Be(0);
    }

    [Fact]
    public void CostUsd_ShouldDefaultToZero()
    {
        // Arrange & Act
        var @event = new TaskFailedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Deployment,
            Complexity = TaskComplexity.Medium,
            Strategy = ExecutionStrategy.Iterative,
            ErrorMessage = "Deployment configuration error",
            Duration = TimeSpan.FromMinutes(3)
        };

        // Assert
        @event.CostUsd.Should().Be(0m);
    }

    [Fact]
    public void TokensUsedAndCost_ShouldAcceptPositiveValues()
    {
        // Arrange & Act
        var @event = new TaskFailedEvent
        {
            TaskId = Guid.NewGuid(),
            TaskType = TaskType.Feature,
            Complexity = TaskComplexity.Complex,
            Strategy = ExecutionStrategy.MultiAgent,
            ErrorMessage = "Runtime error in execution",
            TokensUsed = 12000,
            CostUsd = 0.75m,
            Duration = TimeSpan.FromMinutes(15)
        };

        // Assert
        @event.TokensUsed.Should().Be(12000);
        @event.CostUsd.Should().Be(0.75m);
    }
}
