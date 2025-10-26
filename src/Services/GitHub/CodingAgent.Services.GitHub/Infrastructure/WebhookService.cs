using CodingAgent.Services.GitHub.Domain.Services;
using CodingAgent.Services.GitHub.Domain.Webhooks;
using CodingAgent.SharedKernel.Domain.Events;

namespace CodingAgent.Services.GitHub.Infrastructure;

/// <summary>
/// Service for processing GitHub webhook payloads and converting them to domain events.
/// </summary>
public class WebhookService : IWebhookService
{
    /// <inheritdoc/>
    public GitHubPushEvent ProcessPushWebhook(PushWebhookPayload payload, string webhookId)
    {
        var commit = payload.HeadCommit;
        if (commit == null)
        {
            throw new InvalidOperationException("Push webhook must contain a head commit");
        }

        return new GitHubPushEvent
        {
            WebhookId = webhookId,
            RepositoryOwner = payload.Repository.Owner.Login,
            RepositoryName = payload.Repository.Name,
            Branch = payload.GetBranchName(),
            CommitSha = commit.Id,
            CommitMessage = commit.Message,
            CommitAuthor = commit.Author.Name,
            CommitUrl = commit.Url
        };
    }

    /// <inheritdoc/>
    public GitHubPullRequestEvent ProcessPullRequestWebhook(PullRequestWebhookPayload payload, string webhookId)
    {
        if (string.IsNullOrEmpty(payload.Action))
        {
            throw new InvalidOperationException("Pull request webhook must contain an action");
        }

        return new GitHubPullRequestEvent
        {
            WebhookId = webhookId,
            RepositoryOwner = payload.Repository.Owner.Login,
            RepositoryName = payload.Repository.Name,
            Action = payload.Action,
            PullRequestNumber = payload.PullRequest.Number,
            Title = payload.PullRequest.Title,
            Url = payload.PullRequest.HtmlUrl,
            Author = payload.PullRequest.User.Login,
            Merged = payload.Action == "closed" ? payload.PullRequest.Merged : null
        };
    }

    /// <inheritdoc/>
    public GitHubIssueEvent ProcessIssueWebhook(IssueWebhookPayload payload, string webhookId)
    {
        if (string.IsNullOrEmpty(payload.Action))
        {
            throw new InvalidOperationException("Issue webhook must contain an action");
        }

        // For comment events, use comment author; otherwise use issue author
        var author = payload.Comment?.User.Login ?? payload.Issue.User.Login;
        
        return new GitHubIssueEvent
        {
            WebhookId = webhookId,
            RepositoryOwner = payload.Repository.Owner.Login,
            RepositoryName = payload.Repository.Name,
            Action = payload.Action,
            IssueNumber = payload.Issue.Number,
            Title = payload.Issue.Title,
            Url = payload.Issue.HtmlUrl,
            Author = author,
            CommentBody = payload.Comment?.Body
        };
    }
}
