using CodingAgent.Services.GitHub.Domain.Webhooks;
using CodingAgent.Services.GitHub.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.GitHub.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class WebhookServiceTests
{
    private readonly WebhookService _webhookService;

    public WebhookServiceTests()
    {
        _webhookService = new WebhookService();
    }

    [Fact]
    public void ProcessPushWebhook_WithValidPayload_ReturnsGitHubPushEvent()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new PushWebhookPayload
        {
            Ref = "refs/heads/main",
            After = "abc123def456",
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            HeadCommit = new CommitInfo
            {
                Id = "abc123def456",
                Message = "Test commit message",
                Timestamp = DateTime.UtcNow,
                Url = "https://github.com/testowner/test-repo/commit/abc123def456",
                Author = new AuthorInfo
                {
                    Name = "Test Author",
                    Email = "test@example.com",
                    Username = "testuser"
                }
            }
        };

        // Act
        var result = _webhookService.ProcessPushWebhook(payload, webhookId);

        // Assert
        result.Should().NotBeNull();
        result.WebhookId.Should().Be(webhookId);
        result.RepositoryOwner.Should().Be("testowner");
        result.RepositoryName.Should().Be("test-repo");
        result.Branch.Should().Be("main");
        result.CommitSha.Should().Be("abc123def456");
        result.CommitMessage.Should().Be("Test commit message");
        result.CommitAuthor.Should().Be("Test Author");
        result.CommitUrl.Should().Be("https://github.com/testowner/test-repo/commit/abc123def456");
    }

    [Fact]
    public void ProcessPushWebhook_WithoutHeadCommit_ThrowsInvalidOperationException()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new PushWebhookPayload
        {
            Ref = "refs/heads/main",
            After = "abc123def456",
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            HeadCommit = null
        };

        // Act & Assert
        Action act = () => _webhookService.ProcessPushWebhook(payload, webhookId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Push webhook must contain a head commit");
    }

    [Fact]
    public void ProcessPullRequestWebhook_WithValidPayload_ReturnsGitHubPullRequestEvent()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new PullRequestWebhookPayload
        {
            Action = "opened",
            Number = 42,
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            PullRequest = new PullRequestDetails
            {
                Id = 789,
                Number = 42,
                Title = "Test PR",
                State = "open",
                HtmlUrl = "https://github.com/testowner/test-repo/pull/42",
                User = new UserInfo { Login = "prauthor", Id = 999 },
                Merged = false,
                MergedAt = null
            }
        };

        // Act
        var result = _webhookService.ProcessPullRequestWebhook(payload, webhookId);

        // Assert
        result.Should().NotBeNull();
        result.WebhookId.Should().Be(webhookId);
        result.RepositoryOwner.Should().Be("testowner");
        result.RepositoryName.Should().Be("test-repo");
        result.Action.Should().Be("opened");
        result.PullRequestNumber.Should().Be(42);
        result.Title.Should().Be("Test PR");
        result.Url.Should().Be("https://github.com/testowner/test-repo/pull/42");
        result.Author.Should().Be("prauthor");
        result.Merged.Should().BeNull();
    }

    [Fact]
    public void ProcessPullRequestWebhook_WhenClosed_IncludesMergedStatus()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new PullRequestWebhookPayload
        {
            Action = "closed",
            Number = 42,
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            PullRequest = new PullRequestDetails
            {
                Id = 789,
                Number = 42,
                Title = "Test PR",
                State = "closed",
                HtmlUrl = "https://github.com/testowner/test-repo/pull/42",
                User = new UserInfo { Login = "prauthor", Id = 999 },
                Merged = true,
                MergedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = _webhookService.ProcessPullRequestWebhook(payload, webhookId);

        // Assert
        result.Merged.Should().BeTrue();
    }

    [Fact]
    public void ProcessPullRequestWebhook_WithoutAction_ThrowsInvalidOperationException()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new PullRequestWebhookPayload
        {
            Action = null,
            Number = 42,
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            PullRequest = new PullRequestDetails
            {
                Id = 789,
                Number = 42,
                Title = "Test PR",
                State = "open",
                HtmlUrl = "https://github.com/testowner/test-repo/pull/42",
                User = new UserInfo { Login = "prauthor", Id = 999 },
                Merged = false,
                MergedAt = null
            }
        };

        // Act & Assert
        Action act = () => _webhookService.ProcessPullRequestWebhook(payload, webhookId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Pull request webhook must contain an action");
    }

    [Fact]
    public void ProcessIssueWebhook_WithValidPayload_ReturnsGitHubIssueEvent()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new IssueWebhookPayload
        {
            Action = "opened",
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            Issue = new IssueDetails
            {
                Id = 789,
                Number = 10,
                Title = "Test Issue",
                State = "open",
                HtmlUrl = "https://github.com/testowner/test-repo/issues/10",
                User = new UserInfo { Login = "issueauthor", Id = 999 },
                Body = "Issue description"
            },
            Comment = null
        };

        // Act
        var result = _webhookService.ProcessIssueWebhook(payload, webhookId);

        // Assert
        result.Should().NotBeNull();
        result.WebhookId.Should().Be(webhookId);
        result.RepositoryOwner.Should().Be("testowner");
        result.RepositoryName.Should().Be("test-repo");
        result.Action.Should().Be("opened");
        result.IssueNumber.Should().Be(10);
        result.Title.Should().Be("Test Issue");
        result.Url.Should().Be("https://github.com/testowner/test-repo/issues/10");
        result.Author.Should().Be("issueauthor");
        result.CommentBody.Should().BeNull();
    }

    [Fact]
    public void ProcessIssueWebhook_WithComment_UsesCommentAuthor()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new IssueWebhookPayload
        {
            Action = "created",
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            Issue = new IssueDetails
            {
                Id = 789,
                Number = 10,
                Title = "Test Issue",
                State = "open",
                HtmlUrl = "https://github.com/testowner/test-repo/issues/10",
                User = new UserInfo { Login = "issueauthor", Id = 999 },
                Body = "Issue description"
            },
            Comment = new CommentDetails
            {
                Id = 111,
                Body = "This is a comment",
                User = new UserInfo { Login = "commenter", Id = 222 }
            }
        };

        // Act
        var result = _webhookService.ProcessIssueWebhook(payload, webhookId);

        // Assert
        result.Author.Should().Be("commenter");
        result.CommentBody.Should().Be("This is a comment");
    }

    [Fact]
    public void ProcessIssueWebhook_WithoutAction_ThrowsInvalidOperationException()
    {
        // Arrange
        var webhookId = "test-webhook-id";
        var payload = new IssueWebhookPayload
        {
            Action = null,
            Repository = new RepositoryInfo
            {
                Name = "test-repo",
                FullName = "testowner/test-repo",
                Owner = new UserInfo { Login = "testowner", Id = 123 }
            },
            Sender = new UserInfo { Login = "testuser", Id = 456 },
            Issue = new IssueDetails
            {
                Id = 789,
                Number = 10,
                Title = "Test Issue",
                State = "open",
                HtmlUrl = "https://github.com/testowner/test-repo/issues/10",
                User = new UserInfo { Login = "issueauthor", Id = 999 },
                Body = "Issue description"
            },
            Comment = null
        };

        // Act & Assert
        Action act = () => _webhookService.ProcessIssueWebhook(payload, webhookId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Issue webhook must contain an action");
    }
}
