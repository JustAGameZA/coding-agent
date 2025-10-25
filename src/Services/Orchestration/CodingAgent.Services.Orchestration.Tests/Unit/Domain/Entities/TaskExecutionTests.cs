using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Domain.Entities;

[Trait("Category", "Unit")]
public class TaskExecutionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateExecution()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var strategy = ExecutionStrategy.SingleShot;
        var model = "gpt-4o-mini";

        // Act
        var execution = new TaskExecution(taskId, strategy, model);

        // Assert
        execution.Id.Should().NotBeEmpty();
        execution.TaskId.Should().Be(taskId);
        execution.Strategy.Should().Be(strategy);
        execution.ModelUsed.Should().Be(model);
        execution.Status.Should().Be(ExecutionStatus.Pending);
        execution.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        execution.CompletedAt.Should().BeNull();
        execution.Result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyTaskId_ShouldThrowArgumentException()
    {
        // Arrange
        var taskId = Guid.Empty;
        var strategy = ExecutionStrategy.SingleShot;
        var model = "gpt-4o-mini";

        // Act
        var act = () => new TaskExecution(taskId, strategy, model);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Task ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidModel_ShouldThrowArgumentException(string? invalidModel)
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var strategy = ExecutionStrategy.SingleShot;

        // Act
        var act = () => new TaskExecution(taskId, strategy, invalidModel!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Model used cannot be empty*");
    }

    [Fact]
    public void Complete_WithValidMetrics_ShouldUpdateStatusAndCreateResult()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var tokensUsed = 1500;
        var cost = 0.03m;
        var duration = TimeSpan.FromSeconds(15);

        // Act
        execution.Complete(tokensUsed, cost, duration);

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Success);
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        execution.Result.Should().NotBeNull();
        execution.Result!.Success.Should().BeTrue();
        execution.Result.TokensUsed.Should().Be(tokensUsed);
        execution.Result.CostUSD.Should().Be(cost);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");
        execution.Complete(1500, 0.03m, TimeSpan.FromSeconds(15));

        // Act
        var act = () => execution.Complete(2000, 0.04m, TimeSpan.FromSeconds(20));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot complete execution*");
    }

    [Fact]
    public void Complete_WithNegativeTokens_ShouldThrowArgumentException()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        var act = () => execution.Complete(-100, 0.03m, TimeSpan.FromSeconds(15));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tokens used cannot be negative*");
    }

    [Fact]
    public void Complete_WithNegativeCost_ShouldThrowArgumentException()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        var act = () => execution.Complete(1500, -0.03m, TimeSpan.FromSeconds(15));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Cost cannot be negative*");
    }

    [Fact]
    public void Complete_WithNegativeDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        var act = () => execution.Complete(1500, 0.03m, TimeSpan.FromSeconds(-15));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Duration cannot be negative*");
    }

    [Fact]
    public void Fail_WithValidErrorMessage_ShouldUpdateStatusAndCreateResult()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var errorMessage = "Timeout after 300 seconds";

        // Act
        execution.Fail(errorMessage);

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Failed);
        execution.ErrorMessage.Should().Be(errorMessage);
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        execution.Result.Should().NotBeNull();
        execution.Result!.Success.Should().BeFalse();
        execution.Result.ErrorDetails.Should().Be(errorMessage);
    }

    [Fact]
    public void Fail_WhenAlreadyFailed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");
        execution.Fail("First error");

        // Act
        var act = () => execution.Fail("Second error");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot fail execution*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fail_WithInvalidErrorMessage_ShouldThrowArgumentException(string? invalidMessage)
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        var act = () => execution.Fail(invalidMessage!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Error message cannot be empty*");
    }

    [Fact]
    public void UpdateResult_WithValidResult_ShouldUpdateResult()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var result = new ExecutionResult(execution.Id, true, 2000, 0.04m);

        // Act
        execution.UpdateResult(result);

        // Assert
        execution.Result.Should().Be(result);
    }

    [Fact]
    public void UpdateResult_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        var act = () => execution.UpdateResult(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateResult_WithMismatchedExecutionId_ShouldThrowArgumentException()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);

        // Act
        var act = () => execution.UpdateResult(result);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Result does not belong to this execution*");
    }

    [Fact]
    public void DifferentStrategies_ShouldBeSupported()
    {
        // Arrange & Act
        var singleShot = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");
        var iterative = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.Iterative, "gpt-4o");
        var multiAgent = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.MultiAgent, "gpt-4o");
        var hybrid = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.HybridExecution, "gpt-4o");

        // Assert
        singleShot.Strategy.Should().Be(ExecutionStrategy.SingleShot);
        iterative.Strategy.Should().Be(ExecutionStrategy.Iterative);
        multiAgent.Strategy.Should().Be(ExecutionStrategy.MultiAgent);
        hybrid.Strategy.Should().Be(ExecutionStrategy.HybridExecution);
    }

    [Fact]
    public void StartedAt_ShouldBeSetOnConstruction()
    {
        // Arrange & Act
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Assert
        execution.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CompletedAt_ShouldBeNullUntilCompleteOrFail()
    {
        // Arrange
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Assert
        execution.CompletedAt.Should().BeNull();

        // Act - Complete
        execution.Complete(1500, 0.03m, TimeSpan.FromSeconds(15));

        // Assert
        execution.CompletedAt.Should().NotBeNull();
    }
}
