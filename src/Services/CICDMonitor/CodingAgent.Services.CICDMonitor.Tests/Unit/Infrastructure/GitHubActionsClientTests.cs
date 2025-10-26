using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.ValueObjects;
using CodingAgent.Services.CICDMonitor.Infrastructure.GitHub;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Octokit;

namespace CodingAgent.Services.CICDMonitor.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class GitHubActionsClientTests
{
    private readonly Mock<IGitHubClient> _mockGitHubClient;
    private readonly Mock<ILogger<GitHubActionsClient>> _mockLogger;
    private readonly GitHubActionsClient _sut;

    public GitHubActionsClientTests()
    {
        _mockGitHubClient = new Mock<IGitHubClient>();
        _mockLogger = new Mock<ILogger<GitHubActionsClient>>();
        _sut = new GitHubActionsClient(_mockGitHubClient.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new GitHubActionsClient(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new GitHubActionsClient(_mockGitHubClient.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetRecentWorkflowRunsAsync_WhenApiThrowsException_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var owner = "test-owner";
        var repository = "test-repo";

        var mockActionsClient = new Mock<IActionsClient>();
        var mockWorkflowsClient = new Mock<IActionsWorkflowsClient>();
        var mockRunsClient = new Mock<IActionsWorkflowRunsClient>();

        mockRunsClient
            .Setup(x => x.List(owner, repository))
            .ThrowsAsync(new ApiException("API Error", System.Net.HttpStatusCode.BadRequest));

        mockWorkflowsClient.Setup(x => x.Runs).Returns(mockRunsClient.Object);
        mockActionsClient.Setup(x => x.Workflows).Returns(mockWorkflowsClient.Object);
        _mockGitHubClient.Setup(x => x.Actions).Returns(mockActionsClient.Object);

        // Act
        Func<Task> act = async () => await _sut.GetRecentWorkflowRunsAsync(owner, repository);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to fetch workflow runs*");
    }

    [Fact]
    public async Task GetWorkflowRunLogsAsync_WhenApiThrowsException_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var owner = "test-owner";
        var repository = "test-repo";
        var workflowRunId = 12345L;

        var mockActionsClient = new Mock<IActionsClient>();
        var mockWorkflowsClient = new Mock<IActionsWorkflowsClient>();
        var mockJobsClient = new Mock<IActionsWorkflowJobsClient>();

        mockJobsClient
            .Setup(x => x.List(owner, repository, workflowRunId))
            .ThrowsAsync(new ApiException("API Error", System.Net.HttpStatusCode.Unauthorized));

        mockWorkflowsClient.Setup(x => x.Jobs).Returns(mockJobsClient.Object);
        mockActionsClient.Setup(x => x.Workflows).Returns(mockWorkflowsClient.Object);
        _mockGitHubClient.Setup(x => x.Actions).Returns(mockActionsClient.Object);

        // Act
        Func<Task> act = async () => await _sut.GetWorkflowRunLogsAsync(owner, repository, workflowRunId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to fetch workflow run logs*");
    }
}
