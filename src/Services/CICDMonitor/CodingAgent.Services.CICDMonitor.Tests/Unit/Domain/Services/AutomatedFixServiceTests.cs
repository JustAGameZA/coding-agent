using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using CodingAgent.Services.CICDMonitor.Domain.Services;
using CodingAgent.Services.CICDMonitor.Domain.Services.Implementation;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CodingAgent.Services.CICDMonitor.Tests.Unit.Domain.Services;

[Trait("Category", "Unit")]
public class AutomatedFixServiceTests
{
    private readonly IOrchestrationClient _orchestrationClient;
    private readonly IGitHubClient _githubClient;
    private readonly IFixAttemptRepository _fixAttemptRepository;
    private readonly IBuildFailureRepository _buildFailureRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<AutomatedFixService> _logger;
    private readonly AutomatedFixService _sut;

    public AutomatedFixServiceTests()
    {
        _orchestrationClient = Substitute.For<IOrchestrationClient>();
        _githubClient = Substitute.For<IGitHubClient>();
        _fixAttemptRepository = Substitute.For<IFixAttemptRepository>();
        _buildFailureRepository = Substitute.For<IBuildFailureRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _logger = Substitute.For<ILogger<AutomatedFixService>>();

        _sut = new AutomatedFixService(
            _orchestrationClient,
            _githubClient,
            _fixAttemptRepository,
            _buildFailureRepository,
            _eventPublisher,
            _logger);
    }

    [Fact]
    public void ShouldAttemptFix_WithCompilationError_ReturnsTrue()
    {
        // Arrange
        var errorMessage = "Build failed: error CS0103: The name 'variable' does not exist";

        // Act
        var result = _sut.ShouldAttemptFix(errorMessage);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldAttemptFix_WithTestFailure_ReturnsTrue()
    {
        // Arrange
        var errorMessage = "Test ShouldReturnValidResult failed with exception";

        // Act
        var result = _sut.ShouldAttemptFix(errorMessage);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldAttemptFix_WithUnknownError_ReturnsFalse()
    {
        // Arrange
        var errorMessage = "Some random error that doesn't match any pattern";

        // Act
        var result = _sut.ShouldAttemptFix(errorMessage);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExtractErrorPattern_WithCompilationError_ReturnsCompilationErrorPattern()
    {
        // Arrange
        var errorMessage = "error CS1234: compilation failed";

        // Act
        var pattern = _sut.ExtractErrorPattern(errorMessage);

        // Assert
        pattern.Should().Be("compilation_error");
    }

    [Fact]
    public void ExtractErrorPattern_WithTestFailure_ReturnsTestFailurePattern()
    {
        // Arrange
        var errorMessage = "Test MyTest failed with assertion error";

        // Act
        var pattern = _sut.ExtractErrorPattern(errorMessage);

        // Assert
        pattern.Should().Be("test_failure");
    }

    [Fact]
    public void ExtractErrorPattern_WithMissingDependency_ReturnsMissingDependencyPattern()
    {
        // Arrange
        var errorMessage = "Cannot find module 'express'";

        // Act
        var pattern = _sut.ExtractErrorPattern(errorMessage);

        // Assert
        pattern.Should().Be("missing_dependency");
    }

    [Fact]
    public void ExtractErrorPattern_WithNullReference_ReturnsNullReferencePattern()
    {
        // Arrange
        var errorMessage = "System.NullReferenceException: Object reference not set";

        // Act
        var pattern = _sut.ExtractErrorPattern(errorMessage);

        // Assert
        pattern.Should().Be("null_reference");
    }

    [Fact]
    public void ExtractErrorPattern_WithUnknownError_ReturnsNull()
    {
        // Arrange
        var errorMessage = "Unknown error";

        // Act
        var pattern = _sut.ExtractErrorPattern(errorMessage);

        // Assert
        pattern.Should().BeNull();
    }

    [Fact]
    public async Task ProcessBuildFailureAsync_WithRecognizedError_CreatesFixAttempt()
    {
        // Arrange
        var buildFailure = new BuildFailure
        {
            Id = Guid.NewGuid(),
            Repository = "owner/repo",
            Branch = "main",
            CommitSha = "abc123",
            ErrorMessage = "error CS1234: compilation failed",
            FailedAt = DateTime.UtcNow
        };

        var taskResponse = new CreateTaskResponse
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Fix build error",
            Description = "Fix description",
            Type = SharedKernel.Domain.ValueObjects.TaskType.BugFix,
            Complexity = SharedKernel.Domain.ValueObjects.TaskComplexity.Simple,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _orchestrationClient
            .CreateTaskAsync(Arg.Any<CreateTaskRequest>(), Arg.Any<CancellationToken>())
            .Returns(taskResponse);

        _buildFailureRepository
            .CreateAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>())
            .Returns(buildFailure);

        FixAttempt? capturedFixAttempt = null;
        _fixAttemptRepository
            .CreateAsync(Arg.Do<FixAttempt>(fa => capturedFixAttempt = fa), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<FixAttempt>());

        // Act
        var result = await _sut.ProcessBuildFailureAsync(buildFailure);

        // Assert
        result.Should().NotBeNull();
        result!.BuildFailureId.Should().Be(buildFailure.Id);
        result.TaskId.Should().Be(taskResponse.Id);
        result.Repository.Should().Be("owner/repo");
        result.ErrorPattern.Should().Be("compilation_error");
        result.Status.Should().Be(FixStatus.InProgress);

        await _buildFailureRepository.Received(1).CreateAsync(buildFailure, Arg.Any<CancellationToken>());
        await _orchestrationClient.Received(1).CreateTaskAsync(Arg.Any<CreateTaskRequest>(), Arg.Any<CancellationToken>());
        await _fixAttemptRepository.Received(1).CreateAsync(Arg.Any<FixAttempt>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Any<FixAttemptedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBuildFailureAsync_WithUnrecognizedError_ReturnsNull()
    {
        // Arrange
        var buildFailure = new BuildFailure
        {
            Id = Guid.NewGuid(),
            Repository = "owner/repo",
            Branch = "main",
            CommitSha = "abc123",
            ErrorMessage = "Unknown error that doesn't match any pattern",
            FailedAt = DateTime.UtcNow
        };

        // Act
        var result = await _sut.ProcessBuildFailureAsync(buildFailure);

        // Assert
        result.Should().BeNull();
        await _buildFailureRepository.DidNotReceive().CreateAsync(Arg.Any<BuildFailure>(), Arg.Any<CancellationToken>());
        await _orchestrationClient.DidNotReceive().CreateTaskAsync(Arg.Any<CreateTaskRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTaskCompletionAsync_WithSuccessfulTask_CreatesPRAndUpdatesFixAttempt()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var fixAttempt = new FixAttempt
        {
            Id = Guid.NewGuid(),
            BuildFailureId = Guid.NewGuid(),
            TaskId = taskId,
            Repository = "owner/repo",
            ErrorMessage = "error CS1234",
            ErrorPattern = "compilation_error",
            Status = FixStatus.InProgress,
            AttemptedAt = DateTime.UtcNow,
            BuildFailure = new BuildFailure
            {
                Id = Guid.NewGuid(),
                Repository = "owner/repo",
                Branch = "main",
                CommitSha = "abc123",
                ErrorMessage = "error CS1234",
                FailedAt = DateTime.UtcNow
            }
        };

        var prResponse = new CreatePullRequestResponse
        {
            Id = Guid.NewGuid(),
            Number = 123,
            HtmlUrl = "https://github.com/owner/repo/pull/123",
            Title = "Automated fix",
            Owner = "owner",
            RepositoryName = "repo"
        };

        _fixAttemptRepository
            .GetByTaskIdAsync(taskId, Arg.Any<CancellationToken>())
            .Returns(fixAttempt);

        _githubClient
            .CreatePullRequestAsync(Arg.Any<CreatePullRequestRequest>(), Arg.Any<CancellationToken>())
            .Returns(prResponse);

        // Act
        await _sut.ProcessTaskCompletionAsync(taskId, success: true);

        // Assert
        fixAttempt.Status.Should().Be(FixStatus.Succeeded);
        fixAttempt.PullRequestNumber.Should().Be(123);
        fixAttempt.PullRequestUrl.Should().Be("https://github.com/owner/repo/pull/123");
        fixAttempt.CompletedAt.Should().NotBeNull();

        await _githubClient.Received(1).CreatePullRequestAsync(
            Arg.Is<CreatePullRequestRequest>(r =>
                r.Owner == "owner" &&
                r.Repo == "repo" &&
                r.Base == "main"),
            Arg.Any<CancellationToken>());

        await _fixAttemptRepository.Received(1).UpdateAsync(fixAttempt, Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Any<FixSucceededEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTaskCompletionAsync_WithFailedTask_UpdatesFixAttemptToFailed()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var fixAttempt = new FixAttempt
        {
            Id = Guid.NewGuid(),
            BuildFailureId = Guid.NewGuid(),
            TaskId = taskId,
            Repository = "owner/repo",
            ErrorMessage = "error CS1234",
            ErrorPattern = "compilation_error",
            Status = FixStatus.InProgress,
            AttemptedAt = DateTime.UtcNow
        };

        _fixAttemptRepository
            .GetByTaskIdAsync(taskId, Arg.Any<CancellationToken>())
            .Returns(fixAttempt);

        // Act
        await _sut.ProcessTaskCompletionAsync(taskId, success: false);

        // Assert
        fixAttempt.Status.Should().Be(FixStatus.Failed);
        fixAttempt.FailureReason.Should().Be("Task execution failed");
        fixAttempt.CompletedAt.Should().NotBeNull();

        await _githubClient.DidNotReceive().CreatePullRequestAsync(Arg.Any<CreatePullRequestRequest>(), Arg.Any<CancellationToken>());
        await _fixAttemptRepository.Received(1).UpdateAsync(fixAttempt, Arg.Any<CancellationToken>());
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<FixSucceededEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTaskCompletionAsync_WithNonExistentTask_DoesNothing()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _fixAttemptRepository
            .GetByTaskIdAsync(taskId, Arg.Any<CancellationToken>())
            .Returns((FixAttempt?)null);

        // Act
        await _sut.ProcessTaskCompletionAsync(taskId, success: true);

        // Assert
        await _githubClient.DidNotReceive().CreatePullRequestAsync(Arg.Any<CreatePullRequestRequest>(), Arg.Any<CancellationToken>());
        await _fixAttemptRepository.DidNotReceive().UpdateAsync(Arg.Any<FixAttempt>(), Arg.Any<CancellationToken>());
    }
}
