using CodingAgent.Services.GitHub.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Octokit;

namespace CodingAgent.Services.GitHub.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class GitHubServicePullRequestTests
{
    private readonly Mock<IGitHubClient> _mockClient;
    private readonly Mock<ILogger<GitHubService>> _mockLogger;
    private readonly Mock<IPullRequestsClient> _mockPullRequestsClient;

    public GitHubServicePullRequestTests()
    {
        _mockClient = new Mock<IGitHubClient>();
        _mockLogger = new Mock<ILogger<GitHubService>>();
        _mockPullRequestsClient = new Mock<IPullRequestsClient>();
        _mockClient.Setup(c => c.PullRequest).Returns(_mockPullRequestsClient.Object);
    }

    [Fact]
    public async Task CreatePullRequestAsync_WhenApiThrowsException_ShouldWrapInInvalidOperationException()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        _mockPullRequestsClient
            .Setup(c => c.Create(owner, repo, It.IsAny<NewPullRequest>()))
            .ThrowsAsync(new ApiException("Validation failed", System.Net.HttpStatusCode.UnprocessableEntity));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.CreatePullRequestAsync(owner, repo, "Title", "Body", "head", "base");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to create pull request*");
    }

    [Fact]
    public async Task CreatePullRequestAsync_ShouldCallCreateWithCorrectParameters()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var title = "Test PR";
        var body = "Test body";
        var head = "feature-branch";
        var baseRef = "main";

        _mockPullRequestsClient
            .Setup(c => c.Create(owner, repo, It.IsAny<NewPullRequest>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest)); // Force error for predictable behavior

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.CreatePullRequestAsync(owner, repo, title, body, head, baseRef));

        // Assert - Verify Create was called with correct parameters
        _mockPullRequestsClient.Verify(
            c => c.Create(owner, repo, It.Is<NewPullRequest>(npr =>
                npr.Title == title &&
                npr.Head == head &&
                npr.Base == baseRef &&
                npr.Body == body &&
                npr.Draft == false)),
            Times.Once);
    }

    [Fact]
    public async Task CreatePullRequestAsync_WithDraft_ShouldSetDraftFlag()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";

        _mockPullRequestsClient
            .Setup(c => c.Create(owner, repo, It.IsAny<NewPullRequest>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.CreatePullRequestAsync(owner, repo, "Title", "Body", "head", "base", isDraft: true));

        // Assert
        _mockPullRequestsClient.Verify(
            c => c.Create(owner, repo, It.Is<NewPullRequest>(npr => npr.Draft == true)),
            Times.Once);
    }

    [Fact]
    public async Task GetPullRequestAsync_WhenNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 999;

        _mockPullRequestsClient
            .Setup(c => c.Get(owner, repo, number))
            .ThrowsAsync(new NotFoundException("Not found", System.Net.HttpStatusCode.NotFound));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.GetPullRequestAsync(owner, repo, number);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Pull request not found*");
    }

    [Fact]
    public async Task GetPullRequestAsync_WhenApiThrowsException_ShouldWrapInInvalidOperationException()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 1;

        _mockPullRequestsClient
            .Setup(c => c.Get(owner, repo, number))
            .ThrowsAsync(new ApiException("Server error", System.Net.HttpStatusCode.InternalServerError));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.GetPullRequestAsync(owner, repo, number);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to get pull request*");
    }

    [Fact]
    public async Task ListPullRequestsAsync_WithOpenState_ShouldUseOpenFilter()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var state = "open";

        _mockPullRequestsClient
            .Setup(c => c.GetAllForRepository(owner, repo, It.IsAny<PullRequestRequest>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ListPullRequestsAsync(owner, repo, state));

        // Assert
        _mockPullRequestsClient.Verify(
            c => c.GetAllForRepository(owner, repo, It.Is<PullRequestRequest>(r => r.State == ItemStateFilter.Open)),
            Times.Once);
    }

    [Fact]
    public async Task ListPullRequestsAsync_WithClosedState_ShouldUseClosedFilter()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var state = "closed";

        _mockPullRequestsClient
            .Setup(c => c.GetAllForRepository(owner, repo, It.IsAny<PullRequestRequest>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ListPullRequestsAsync(owner, repo, state));

        // Assert
        _mockPullRequestsClient.Verify(
            c => c.GetAllForRepository(owner, repo, It.Is<PullRequestRequest>(r => r.State == ItemStateFilter.Closed)),
            Times.Once);
    }

    [Fact]
    public async Task MergePullRequestAsync_WithSquashMethod_ShouldUseSquashMergeMethod()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 5;
        var mergeMethod = "squash";

        _mockPullRequestsClient
            .Setup(c => c.Merge(owner, repo, number, It.IsAny<MergePullRequest>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.MergePullRequestAsync(owner, repo, number, mergeMethod));

        // Assert
        _mockPullRequestsClient.Verify(
            c => c.Merge(owner, repo, number, It.Is<MergePullRequest>(m => m.MergeMethod == PullRequestMergeMethod.Squash)),
            Times.Once);
    }

    [Fact]
    public async Task MergePullRequestAsync_WithRebaseMethod_ShouldUseRebaseMergeMethod()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 5;
        var mergeMethod = "rebase";

        _mockPullRequestsClient
            .Setup(c => c.Merge(owner, repo, number, It.IsAny<MergePullRequest>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.MergePullRequestAsync(owner, repo, number, mergeMethod));

        // Assert
        _mockPullRequestsClient.Verify(
            c => c.Merge(owner, repo, number, It.Is<MergePullRequest>(m => m.MergeMethod == PullRequestMergeMethod.Rebase)),
            Times.Once);
    }

    [Fact]
    public async Task MergePullRequestAsync_WhenMergeFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 5;

        var mergeResult = new PullRequestMerge(
            sha: "",
            merged: false,
            message: "Merge conflict");

        _mockPullRequestsClient
            .Setup(c => c.Merge(owner, repo, number, It.IsAny<MergePullRequest>()))
            .ReturnsAsync(mergeResult);

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await sut.MergePullRequestAsync(owner, repo, number);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Pull request was not merged*");
    }

    [Fact]
    public async Task ClosePullRequestAsync_ShouldCallUpdateWithClosedState()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 7;

        _mockPullRequestsClient
            .Setup(c => c.Update(owner, repo, number, It.IsAny<PullRequestUpdate>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ClosePullRequestAsync(owner, repo, number));

        // Assert
        _mockPullRequestsClient.Verify(
            c => c.Update(owner, repo, number, It.Is<PullRequestUpdate>(u => u.State == ItemState.Closed)),
            Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldCallIssueCommentCreate()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 8;
        var comment = "This looks great!";

        var mockIssueClient = new Mock<IIssuesClient>();
        var mockCommentClient = new Mock<IIssueCommentsClient>();

        mockIssueClient.Setup(c => c.Comment).Returns(mockCommentClient.Object);
        _mockClient.Setup(c => c.Issue).Returns(mockIssueClient.Object);

        mockCommentClient
            .Setup(c => c.Create(owner, repo, number, comment))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.AddCommentAsync(owner, repo, number, comment));

        // Assert
        mockCommentClient.Verify(c => c.Create(owner, repo, number, comment), Times.Once);
    }

    [Fact]
    public async Task RequestReviewAsync_ShouldCallReviewRequestCreate()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 9;
        var reviewers = new[] { "reviewer1", "reviewer2" };

        var mockReviewRequestClient = new Mock<IPullRequestReviewRequestsClient>();
        _mockPullRequestsClient.Setup(c => c.ReviewRequest).Returns(mockReviewRequestClient.Object);

        mockReviewRequestClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<PullRequestReviewRequest>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.RequestReviewAsync(owner, repo, number, reviewers));

        // Assert
        mockReviewRequestClient.Verify(
            c => c.Create(owner, repo, number, It.Is<PullRequestReviewRequest>(r => r.Reviewers.Count == 2)),
            Times.Once);
    }

    [Fact]
    public async Task ApprovePullRequestAsync_ShouldCallReviewCreateWithApprove()
    {
        // Arrange
        var owner = "testowner";
        var repo = "testrepo";
        var number = 10;
        var body = "LGTM!";

        var mockReviewClient = new Mock<IPullRequestReviewsClient>();
        _mockPullRequestsClient.Setup(c => c.Review).Returns(mockReviewClient.Object);

        mockReviewClient
            .Setup(c => c.Create(owner, repo, number, It.IsAny<PullRequestReviewCreate>()))
            .ThrowsAsync(new ApiException("", System.Net.HttpStatusCode.BadRequest));

        var sut = new GitHubService(_mockClient.Object, _mockLogger.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ApprovePullRequestAsync(owner, repo, number, body));

        // Assert
        mockReviewClient.Verify(
            c => c.Create(owner, repo, number, It.Is<PullRequestReviewCreate>(r => r.Event == PullRequestReviewEvent.Approve)),
            Times.Once);
    }
}
