using CodingAgent.Services.Orchestration.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Unit.Domain.Entities;

[Trait("Category", "Unit")]
public class ExecutionResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateResult()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var success = true;
        var tokensUsed = 2000;
        var costUSD = 0.04m;

        // Act
        var result = new ExecutionResult(executionId, success, tokensUsed, costUSD);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.ExecutionId.Should().Be(executionId);
        result.Success.Should().Be(success);
        result.TokensUsed.Should().Be(tokensUsed);
        result.CostUSD.Should().Be(costUSD);
        result.Changes.Should().BeNull();
        result.ErrorDetails.Should().BeNull();
        result.FilesChanged.Should().Be(0);
        result.LinesAdded.Should().Be(0);
        result.LinesRemoved.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithEmptyExecutionId_ShouldThrowArgumentException()
    {
        // Arrange
        var executionId = Guid.Empty;

        // Act
        var act = () => new ExecutionResult(executionId, true, 2000, 0.04m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution ID cannot be empty*");
    }

    [Fact]
    public void Constructor_WithNegativeTokens_ShouldThrowArgumentException()
    {
        // Arrange
        var executionId = Guid.NewGuid();

        // Act
        var act = () => new ExecutionResult(executionId, true, -100, 0.04m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tokens used cannot be negative*");
    }

    [Fact]
    public void Constructor_WithNegativeCost_ShouldThrowArgumentException()
    {
        // Arrange
        var executionId = Guid.NewGuid();

        // Act
        var act = () => new ExecutionResult(executionId, true, 2000, -0.04m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Cost cannot be negative*");
    }

    [Fact]
    public void SetChanges_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);
        var changes = "Modified 3 files:\n- src/Login.cs\n- src/Auth.cs\n- tests/LoginTests.cs";
        var filesChanged = 3;
        var linesAdded = 45;
        var linesRemoved = 12;

        // Act
        result.SetChanges(changes, filesChanged, linesAdded, linesRemoved);

        // Assert
        result.Changes.Should().Be(changes);
        result.FilesChanged.Should().Be(filesChanged);
        result.LinesAdded.Should().Be(linesAdded);
        result.LinesRemoved.Should().Be(linesRemoved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetChanges_WithInvalidChanges_ShouldThrowArgumentException(string? invalidChanges)
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);

        // Act
        var act = () => result.SetChanges(invalidChanges!, 3, 45, 12);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Changes cannot be empty*");
    }

    [Fact]
    public void SetChanges_WithNegativeFilesChanged_ShouldThrowArgumentException()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);

        // Act
        var act = () => result.SetChanges("Changes", -1, 45, 12);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Files changed cannot be negative*");
    }

    [Fact]
    public void SetChanges_WithNegativeLinesAdded_ShouldThrowArgumentException()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);

        // Act
        var act = () => result.SetChanges("Changes", 3, -45, 12);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Lines added cannot be negative*");
    }

    [Fact]
    public void SetChanges_WithNegativeLinesRemoved_ShouldThrowArgumentException()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);

        // Act
        var act = () => result.SetChanges("Changes", 3, 45, -12);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Lines removed cannot be negative*");
    }

    [Fact]
    public void SetError_WithValidErrorDetails_ShouldUpdatePropertiesAndMarkAsFailure()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 0, 0m);
        var errorDetails = "Compilation failed: missing semicolon at line 42";

        // Act
        result.SetError(errorDetails);

        // Assert
        result.ErrorDetails.Should().Be(errorDetails);
        result.Success.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetError_WithInvalidErrorDetails_ShouldThrowArgumentException(string? invalidError)
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 0, 0m);

        // Act
        var act = () => result.SetError(invalidError!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Error details cannot be empty*");
    }

    [Fact]
    public void SuccessfulResult_WithChanges_ShouldHaveCompleteInformation()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var result = new ExecutionResult(executionId, true, 3500, 0.07m);

        // Act
        result.SetChanges(
            changes: "Fixed login bug by updating authentication flow",
            filesChanged: 2,
            linesAdded: 35,
            linesRemoved: 8
        );

        // Assert
        result.Success.Should().BeTrue();
        result.TokensUsed.Should().Be(3500);
        result.CostUSD.Should().Be(0.07m);
        result.FilesChanged.Should().Be(2);
        result.LinesAdded.Should().Be(35);
        result.LinesRemoved.Should().Be(8);
        result.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void FailedResult_WithError_ShouldIndicateFailure()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var result = new ExecutionResult(executionId, false, 1200, 0.02m);
        var errorDetails = "API rate limit exceeded";

        // Act
        result.SetError(errorDetails);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorDetails.Should().Be(errorDetails);
        result.Changes.Should().BeNull();
    }

    [Fact]
    public void ZeroMetrics_ShouldBeValid()
    {
        // Arrange & Act
        var result = new ExecutionResult(Guid.NewGuid(), false, 0, 0m);

        // Assert
        result.TokensUsed.Should().Be(0);
        result.CostUSD.Should().Be(0m);
        result.FilesChanged.Should().Be(0);
        result.LinesAdded.Should().Be(0);
        result.LinesRemoved.Should().Be(0);
    }

    [Fact]
    public void UpdatedAt_ShouldChangeAfterSetChanges()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);
        var originalUpdatedAt = result.UpdatedAt;

        // Act
        Thread.Sleep(100); // Ensure time passes
        result.SetChanges("Changes", 2, 30, 10);

        // Assert
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdatedAt_ShouldChangeAfterSetError()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 0, 0m);
        var originalUpdatedAt = result.UpdatedAt;

        // Act
        Thread.Sleep(100); // Ensure time passes
        result.SetError("Error occurred");

        // Assert
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void CreatedAt_ShouldNotChangeAfterUpdates()
    {
        // Arrange
        var result = new ExecutionResult(Guid.NewGuid(), true, 2000, 0.04m);
        var createdAt = result.CreatedAt;

        // Act
        result.SetChanges("Changes", 2, 30, 10);
        result.SetError("Error");

        // Assert
        result.CreatedAt.Should().Be(createdAt);
    }
}
