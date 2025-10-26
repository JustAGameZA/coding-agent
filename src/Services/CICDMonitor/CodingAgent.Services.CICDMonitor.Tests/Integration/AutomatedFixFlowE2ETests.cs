using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using CodingAgent.Services.CICDMonitor.Domain.Services;
using CodingAgent.Services.CICDMonitor.Domain.Services.Implementation;
using CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;
using CodingAgent.SharedKernel.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CodingAgent.Services.CICDMonitor.Tests.Integration;

[Trait("Category", "Integration")]
public class AutomatedFixFlowE2ETests : IAsyncLifetime
{
    private CICDMonitorDbContext _dbContext = default!;
    private IAutomatedFixService _automatedFixService = default!;
    private IOrchestrationClient _orchestrationClient = default!;
    private IGitHubClient _githubClient = default!;
    private IEventPublisher _eventPublisher = default!;
    private IFixAttemptRepository _fixAttemptRepository = default!;
    private IBuildFailureRepository _buildFailureRepository = default!;

    public async Task InitializeAsync()
    {
        // Use in-memory database for tests
        var options = new DbContextOptionsBuilder<CICDMonitorDbContext>()
            .UseInMemoryDatabase(databaseName: $"CICDMonitorTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new CICDMonitorDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Setup repositories
        _buildFailureRepository = new BuildFailureRepository(_dbContext);
        _fixAttemptRepository = new FixAttemptRepository(_dbContext);

        // Setup mocked external services
        _orchestrationClient = Substitute.For<IOrchestrationClient>();
        _githubClient = Substitute.For<IGitHubClient>();
        _eventPublisher = Substitute.For<IEventPublisher>();

        // Setup automated fix service
        var logger = Substitute.For<ILogger<AutomatedFixService>>();
        _automatedFixService = new AutomatedFixService(
            _orchestrationClient,
            _githubClient,
            _fixAttemptRepository,
            _buildFailureRepository,
            _eventPublisher,
            logger);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task EndToEndFlow_BuildFailureToSuccessfulPR_CompletesSuccessfully()
    {
        // Arrange: Simulate a build failure
        var buildFailure = new BuildFailure
        {
            Id = Guid.NewGuid(),
            Repository = "owner/test-repo",
            Branch = "main",
            CommitSha = "abc123def456",
            ErrorMessage = "error CS1234: The name 'undefined' does not exist in the current context",
            ErrorLog = "Full build log with stack trace...",
            WorkflowName = "CI Build",
            JobName = "build",
            FailedAt = DateTime.UtcNow
        };

        var taskResponse = new CreateTaskResponse
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Fix build error",
            Description = "Fix description",
            Type = TaskType.BugFix,
            Complexity = TaskComplexity.Simple,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var prResponse = new CreatePullRequestResponse
        {
            Id = Guid.NewGuid(),
            Number = 42,
            HtmlUrl = "https://github.com/owner/test-repo/pull/42",
            Title = "Automated fix for build failure",
            Owner = "owner",
            RepositoryName = "test-repo"
        };

        _orchestrationClient
            .CreateTaskAsync(Arg.Any<CreateTaskRequest>(), Arg.Any<CancellationToken>())
            .Returns(taskResponse);

        _githubClient
            .CreatePullRequestAsync(Arg.Any<CreatePullRequestRequest>(), Arg.Any<CancellationToken>())
            .Returns(prResponse);

        // Act Step 1: Process build failure
        var fixAttempt = await _automatedFixService.ProcessBuildFailureAsync(buildFailure);

        // Assert Step 1: Fix attempt created
        fixAttempt.Should().NotBeNull();
        fixAttempt!.Status.Should().Be(FixStatus.InProgress);
        fixAttempt.TaskId.Should().Be(taskResponse.Id);
        fixAttempt.ErrorPattern.Should().Be("compilation_error");

        // Verify build failure was saved to database
        var savedBuildFailure = await _buildFailureRepository.GetByIdAsync(buildFailure.Id);
        savedBuildFailure.Should().NotBeNull();
        savedBuildFailure!.ErrorMessage.Should().Be(buildFailure.ErrorMessage);

        // Verify fix attempt was saved to database
        var savedFixAttempt = await _fixAttemptRepository.GetByTaskIdAsync(taskResponse.Id);
        savedFixAttempt.Should().NotBeNull();
        savedFixAttempt!.Status.Should().Be(FixStatus.InProgress);

        // Verify FixAttemptedEvent was published
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<FixAttemptedEvent>(e =>
                e.FixAttemptId == fixAttempt.Id &&
                e.TaskId == taskResponse.Id &&
                e.Repository == "owner/test-repo"),
            Arg.Any<CancellationToken>());

        // Act Step 2: Simulate task completion with success
        await _automatedFixService.ProcessTaskCompletionAsync(taskResponse.Id, success: true);

        // Assert Step 2: Fix succeeded and PR created
        var updatedFixAttempt = await _fixAttemptRepository.GetByTaskIdAsync(taskResponse.Id);
        updatedFixAttempt.Should().NotBeNull();
        updatedFixAttempt!.Status.Should().Be(FixStatus.Succeeded);
        updatedFixAttempt.PullRequestNumber.Should().Be(42);
        updatedFixAttempt.PullRequestUrl.Should().Be("https://github.com/owner/test-repo/pull/42");
        updatedFixAttempt.CompletedAt.Should().NotBeNull();

        // Verify PR was created with correct parameters
        await _githubClient.Received(1).CreatePullRequestAsync(
            Arg.Is<CreatePullRequestRequest>(r =>
                r.Owner == "owner" &&
                r.Repo == "test-repo" &&
                r.Base == "main" &&
                r.Head.Contains("automated-fix")),
            Arg.Any<CancellationToken>());

        // Verify FixSucceededEvent was published
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<FixSucceededEvent>(e =>
                e.FixAttemptId == fixAttempt.Id &&
                e.TaskId == taskResponse.Id &&
                e.PullRequestNumber == 42 &&
                e.Repository == "owner/test-repo"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndToEndFlow_BuildFailureToFailedTask_MarksFixAsFailed()
    {
        // Arrange: Simulate a build failure
        var buildFailure = new BuildFailure
        {
            Id = Guid.NewGuid(),
            Repository = "owner/test-repo",
            Branch = "develop",
            CommitSha = "xyz789",
            ErrorMessage = "Test TestSample failed with NullReferenceException",
            FailedAt = DateTime.UtcNow
        };

        var taskResponse = new CreateTaskResponse
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Fix test failure",
            Description = "Fix description",
            Type = TaskType.BugFix,
            Complexity = TaskComplexity.Medium,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _orchestrationClient
            .CreateTaskAsync(Arg.Any<CreateTaskRequest>(), Arg.Any<CancellationToken>())
            .Returns(taskResponse);

        // Act Step 1: Process build failure
        var fixAttempt = await _automatedFixService.ProcessBuildFailureAsync(buildFailure);

        // Assert Step 1: Fix attempt created
        fixAttempt.Should().NotBeNull();
        fixAttempt!.Status.Should().Be(FixStatus.InProgress);

        // Act Step 2: Simulate task completion with failure
        await _automatedFixService.ProcessTaskCompletionAsync(taskResponse.Id, success: false);

        // Assert Step 2: Fix marked as failed
        var updatedFixAttempt = await _fixAttemptRepository.GetByTaskIdAsync(taskResponse.Id);
        updatedFixAttempt.Should().NotBeNull();
        updatedFixAttempt!.Status.Should().Be(FixStatus.Failed);
        updatedFixAttempt.FailureReason.Should().Be("Task execution failed");
        updatedFixAttempt.PullRequestNumber.Should().BeNull();
        updatedFixAttempt.CompletedAt.Should().NotBeNull();

        // Verify no PR was created
        await _githubClient.DidNotReceive().CreatePullRequestAsync(
            Arg.Any<CreatePullRequestRequest>(),
            Arg.Any<CancellationToken>());

        // Verify FixSucceededEvent was NOT published
        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<FixSucceededEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatistics_WithMultipleAttempts_ReturnsCorrectMetrics()
    {
        // Arrange: Create multiple fix attempts with different statuses
        var buildFailure1 = new BuildFailure
        {
            Id = Guid.NewGuid(),
            Repository = "owner/repo1",
            Branch = "main",
            CommitSha = "abc",
            ErrorMessage = "error CS1234",
            FailedAt = DateTime.UtcNow
        };

        var buildFailure2 = new BuildFailure
        {
            Id = Guid.NewGuid(),
            Repository = "owner/repo2",
            Branch = "main",
            CommitSha = "def",
            ErrorMessage = "Test failed",
            FailedAt = DateTime.UtcNow
        };

        await _buildFailureRepository.CreateAsync(buildFailure1);
        await _buildFailureRepository.CreateAsync(buildFailure2);

        var fixAttempt1 = new FixAttempt
        {
            Id = Guid.NewGuid(),
            BuildFailureId = buildFailure1.Id,
            TaskId = Guid.NewGuid(),
            Repository = "owner/repo1",
            ErrorMessage = "error CS1234",
            ErrorPattern = "compilation_error",
            Status = FixStatus.Succeeded,
            AttemptedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            PullRequestNumber = 1,
            PullRequestUrl = "https://github.com/owner/repo1/pull/1"
        };

        var fixAttempt2 = new FixAttempt
        {
            Id = Guid.NewGuid(),
            BuildFailureId = buildFailure2.Id,
            TaskId = Guid.NewGuid(),
            Repository = "owner/repo2",
            ErrorMessage = "Test failed",
            ErrorPattern = "test_failure",
            Status = FixStatus.Failed,
            AttemptedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            FailureReason = "Task failed"
        };

        var fixAttempt3 = new FixAttempt
        {
            Id = Guid.NewGuid(),
            BuildFailureId = buildFailure1.Id,
            TaskId = Guid.NewGuid(),
            Repository = "owner/repo1",
            ErrorMessage = "error CS5678",
            ErrorPattern = "compilation_error",
            Status = FixStatus.InProgress,
            AttemptedAt = DateTime.UtcNow
        };

        await _fixAttemptRepository.CreateAsync(fixAttempt1);
        await _fixAttemptRepository.CreateAsync(fixAttempt2);
        await _fixAttemptRepository.CreateAsync(fixAttempt3);

        // Act: Get overall statistics
        var overallStats = await _fixAttemptRepository.GetStatisticsAsync();

        // Assert: Overall statistics
        overallStats.TotalAttempts.Should().Be(3);
        overallStats.Succeeded.Should().Be(1);
        overallStats.Failed.Should().Be(1);
        overallStats.InProgress.Should().Be(1);
        overallStats.SuccessRate.Should().BeApproximately(33.33, 0.01);

        // Act: Get statistics by error pattern
        var patternStats = await _fixAttemptRepository.GetStatisticsByErrorPatternAsync();

        // Assert: Pattern statistics
        patternStats.Should().ContainKey("compilation_error");
        patternStats["compilation_error"].TotalAttempts.Should().Be(2);
        patternStats["compilation_error"].Succeeded.Should().Be(1);
        patternStats["compilation_error"].InProgress.Should().Be(1);
        patternStats["compilation_error"].SuccessRate.Should().Be(50.0);

        patternStats.Should().ContainKey("test_failure");
        patternStats["test_failure"].TotalAttempts.Should().Be(1);
        patternStats["test_failure"].Failed.Should().Be(1);
        patternStats["test_failure"].SuccessRate.Should().Be(0.0);
    }
}
