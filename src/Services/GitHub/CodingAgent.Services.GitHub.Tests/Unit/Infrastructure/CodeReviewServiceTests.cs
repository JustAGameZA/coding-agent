using CodingAgent.Services.GitHub.Infrastructure;
using CodingAgent.Services.GitHub.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Octokit;

namespace CodingAgent.Services.GitHub.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class CodeReviewServiceTests
{
    private readonly Mock<IGitHubClient> _mockClient;
    private readonly Mock<ILogger<CodeReviewService>> _mockLogger;
    private readonly Mock<IPullRequestsClient> _mockPullRequestsClient;
    private readonly Mock<IIssuesClient> _mockIssuesClient;
    private readonly Mock<IIssueCommentsClient> _mockIssueCommentsClient;
    private readonly Mock<IPullRequestReviewsClient> _mockReviewsClient;

    public CodeReviewServiceTests()
    {
        _mockClient = new Mock<IGitHubClient>();
        _mockLogger = new Mock<ILogger<CodeReviewService>>();
        _mockPullRequestsClient = new Mock<IPullRequestsClient>();
        _mockIssuesClient = new Mock<IIssuesClient>();
        _mockIssueCommentsClient = new Mock<IIssueCommentsClient>();
        _mockReviewsClient = new Mock<IPullRequestReviewsClient>();

        _mockClient.Setup(c => c.PullRequest).Returns(_mockPullRequestsClient.Object);
        _mockClient.Setup(c => c.Issue).Returns(_mockIssuesClient.Object);
        _mockIssuesClient.Setup(c => c.Comment).Returns(_mockIssueCommentsClient.Object);
        _mockPullRequestsClient.Setup(c => c.Review).Returns(_mockReviewsClient.Object);
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new CodeReviewService(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new CodeReviewService(_mockClient.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task AnalyzePullRequestAsync_WhenApiThrowsException_ShouldWrapInInvalidOperationException()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 1;

        _mockPullRequestsClient
            .Setup(c => c.Get(owner, repo, number))
            .ThrowsAsync(new ApiException("Error", System.Net.HttpStatusCode.InternalServerError));

        var sut = new CodeReviewService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.AnalyzePullRequestAsync(owner, repo, number);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to analyze pull request*");
    }

    [Fact]
    public async Task PostReviewCommentsAsync_WithNoIssues_ShouldPostApprovalComment()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 1;
        var result = new CodeReviewResult
        {
            RequestChanges = false,
            Issues = new List<CodeReviewIssue>(),
            Summary = "No issues"
        };

        _mockIssueCommentsClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<string>()))
            .ReturnsAsync((IssueComment)null!);

        var sut = new CodeReviewService(_mockClient.Object, _mockLogger.Object);

        // Act
        await sut.PostReviewCommentsAsync(owner, repo, number, result);

        // Assert
        _mockIssueCommentsClient.Verify(
            c => c.Create(owner, repo, number, It.Is<string>(s => s.Contains("No issues detected"))),
            Times.Once);
    }

    [Fact]
    public async Task PostReviewCommentsAsync_WithIssues_ShouldPostReview()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 1;
        var result = new CodeReviewResult
        {
            RequestChanges = false,
            Issues = new List<CodeReviewIssue>
            {
                new CodeReviewIssue
                {
                    Severity = "warning",
                    IssueType = "large_pr",
                    FilePath = "test.cs",
                    Description = "Large PR detected"
                }
            },
            Summary = "Issues detected"
        };

        _mockReviewsClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<PullRequestReviewCreate>()))
            .ReturnsAsync((PullRequestReview)null!);

        _mockIssueCommentsClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<string>()))
            .ReturnsAsync((IssueComment)null!);

        var sut = new CodeReviewService(_mockClient.Object, _mockLogger.Object);

        // Act
        await sut.PostReviewCommentsAsync(owner, repo, number, result);

        // Assert
        _mockReviewsClient.Verify(
            c => c.Create(owner, repo, number, It.Is<PullRequestReviewCreate>(r => r.Event == PullRequestReviewEvent.Comment)),
            Times.Once);
    }

    [Fact]
    public async Task PostReviewCommentsAsync_WithCriticalIssues_ShouldRequestChanges()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 1;
        var result = new CodeReviewResult
        {
            RequestChanges = true,
            Issues = new List<CodeReviewIssue>
            {
                new CodeReviewIssue
                {
                    Severity = "error",
                    IssueType = "critical_issue",
                    FilePath = "test.cs",
                    Description = "Critical issue detected"
                }
            },
            Summary = "Critical issues"
        };

        _mockReviewsClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<PullRequestReviewCreate>()))
            .ReturnsAsync((PullRequestReview)null!);

        _mockIssueCommentsClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<string>()))
            .ReturnsAsync((IssueComment)null!);

        var sut = new CodeReviewService(_mockClient.Object, _mockLogger.Object);

        // Act
        await sut.PostReviewCommentsAsync(owner, repo, number, result);

        // Assert
        _mockReviewsClient.Verify(
            c => c.Create(owner, repo, number, It.Is<PullRequestReviewCreate>(r => r.Event == PullRequestReviewEvent.RequestChanges)),
            Times.Once);
    }

    [Fact]
    public async Task PostReviewCommentsAsync_WhenApiThrowsException_ShouldWrapInInvalidOperationException()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 1;
        var result = new CodeReviewResult
        {
            RequestChanges = false,
            Issues = new List<CodeReviewIssue>(),
            Summary = "No issues"
        };

        _mockIssueCommentsClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<string>()))
            .ThrowsAsync(new ApiException("Error", System.Net.HttpStatusCode.InternalServerError));

        var sut = new CodeReviewService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.PostReviewCommentsAsync(owner, repo, number, result);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to post review comments*");
    }
}
