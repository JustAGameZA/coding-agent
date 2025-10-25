using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using FluentAssertions;
using Xunit;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Domain.Entities;

[Trait("Category", "Unit")]
public class CodingTaskTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTask()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var title = "Fix login bug";
        var description = "Users can't login after password reset";

        // Act
        var task = new CodingTask(userId, title, description);

        // Assert
        task.Id.Should().NotBeEmpty();
        task.UserId.Should().Be(userId);
        task.Title.Should().Be(title);
        task.Description.Should().Be(description);
        task.Status.Should().Be(TaskStatus.Pending);
        task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        task.Executions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.Empty;
        var title = "Fix bug";
        var description = "Description";

        // Act
        var act = () => new CodingTask(userId, title, description);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var description = "Description";

        // Act
        var act = () => new CodingTask(userId, invalidTitle!, description);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDescription_ShouldThrowArgumentException(string? invalidDescription)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var title = "Title";

        // Act
        var act = () => new CodingTask(userId, title, invalidDescription!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Description cannot be empty*");
    }

    [Fact]
    public void Classify_FromPending_ShouldUpdateTypeComplexityAndStatus()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");

        // Act
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);

        // Assert
        task.Type.Should().Be(TaskType.BugFix);
        task.Complexity.Should().Be(TaskComplexity.Simple);
        task.Status.Should().Be(TaskStatus.Classifying);
    }

    [Fact]
    public void Classify_FromCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();
        task.Complete();

        // Act
        var act = () => task.Classify(TaskType.Feature, TaskComplexity.Medium);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid status transition*");
    }

    [Fact]
    public void Start_FromClassifying_ShouldUpdateStatusAndStartedAt()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);

        // Act
        task.Start();

        // Assert
        task.Status.Should().Be(TaskStatus.InProgress);
        task.StartedAt.Should().NotBeNull();
        task.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Start_FromPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");

        // Act
        var act = () => task.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid status transition*");
    }

    [Fact]
    public void Complete_FromInProgress_ShouldUpdateStatusAndCompletedAt()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();

        // Act
        task.Complete();

        // Assert
        task.Status.Should().Be(TaskStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
        task.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_FromCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();
        task.Complete();

        // Act
        var act = () => task.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid status transition*");
    }

    [Fact]
    public void Fail_FromInProgress_ShouldUpdateStatusAndCompletedAt()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();
        var errorMessage = "Execution timeout";

        // Act
        task.Fail(errorMessage);

        // Assert
        task.Status.Should().Be(TaskStatus.Failed);
        task.CompletedAt.Should().NotBeNull();
        task.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fail_WithInvalidErrorMessage_ShouldThrowArgumentException(string? invalidMessage)
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();

        // Act
        var act = () => task.Fail(invalidMessage!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Error message cannot be empty*");
    }

    [Fact]
    public void Cancel_FromPending_ShouldUpdateStatusAndCompletedAt()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");

        // Act
        task.Cancel();

        // Assert
        task.Status.Should().Be(TaskStatus.Cancelled);
        task.CompletedAt.Should().NotBeNull();
        task.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Cancel_FromInProgress_ShouldUpdateStatusAndCompletedAt()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();

        // Act
        task.Cancel();

        // Assert
        task.Status.Should().Be(TaskStatus.Cancelled);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_FromCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();
        task.Complete();

        // Act
        var act = () => task.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid status transition*");
    }

    [Fact]
    public void AddExecution_WithValidExecution_ShouldAddToCollection()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        var execution = new TaskExecution(task.Id, ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        task.AddExecution(execution);

        // Assert
        task.Executions.Should().HaveCount(1);
        task.Executions.Should().Contain(execution);
    }

    [Fact]
    public void AddExecution_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");

        // Act
        var act = () => task.AddExecution(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddExecution_WithMismatchedTaskId_ShouldThrowArgumentException()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        var execution = new TaskExecution(Guid.NewGuid(), ExecutionStrategy.SingleShot, "gpt-4o-mini");

        // Act
        var act = () => task.AddExecution(execution);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution does not belong to this task*");
    }

    [Fact]
    public void StateTransitions_FollowValidFlow()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");

        // Act & Assert - Valid flow: Pending -> Classifying -> InProgress -> Completed
        task.Status.Should().Be(TaskStatus.Pending);

        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Status.Should().Be(TaskStatus.Classifying);

        task.Start();
        task.Status.Should().Be(TaskStatus.InProgress);

        task.Complete();
        task.Status.Should().Be(TaskStatus.Completed);
    }

    [Fact]
    public void StateTransitions_FailureFlow()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");

        // Act & Assert - Failure flow: Pending -> Classifying -> InProgress -> Failed
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();
        task.Fail("Error occurred");

        task.Status.Should().Be(TaskStatus.Failed);
    }

    [Fact]
    public void Executions_Collection_ShouldBeReadOnly()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");

        // Act
        var executions = task.Executions;

        // Assert
        executions.Should().BeAssignableTo<IReadOnlyCollection<TaskExecution>>();
    }

    [Fact]
    public void CreatedAt_ShouldNotChangeAfterUpdates()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        var createdAt = task.CreatedAt;

        // Act
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);
        task.Start();

        // Assert
        task.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void UpdatedAt_ShouldChangeAfterModifications()
    {
        // Arrange
        var task = new CodingTask(Guid.NewGuid(), "Title", "Description");
        var originalUpdatedAt = task.UpdatedAt;

        // Act
        Thread.Sleep(100); // Ensure time passes
        task.Classify(TaskType.BugFix, TaskComplexity.Simple);

        // Assert
        task.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
