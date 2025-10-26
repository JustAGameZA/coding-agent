using CodingAgent.Services.GitHub.Domain.Webhooks;
using CodingAgent.SharedKernel.Domain.Events;

namespace CodingAgent.Services.GitHub.Domain.Services;

/// <summary>
/// Service for processing GitHub webhook payloads.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Processes a push webhook payload and returns a domain event.
    /// </summary>
    /// <param name="payload">The push webhook payload.</param>
    /// <param name="webhookId">The webhook delivery ID.</param>
    /// <returns>A GitHubPushEvent.</returns>
    GitHubPushEvent ProcessPushWebhook(PushWebhookPayload payload, string webhookId);

    /// <summary>
    /// Processes a pull request webhook payload and returns a domain event.
    /// </summary>
    /// <param name="payload">The pull request webhook payload.</param>
    /// <param name="webhookId">The webhook delivery ID.</param>
    /// <returns>A GitHubPullRequestEvent.</returns>
    GitHubPullRequestEvent ProcessPullRequestWebhook(PullRequestWebhookPayload payload, string webhookId);

    /// <summary>
    /// Processes an issue webhook payload and returns a domain event.
    /// </summary>
    /// <param name="payload">The issue webhook payload.</param>
    /// <param name="webhookId">The webhook delivery ID.</param>
    /// <returns>A GitHubIssueEvent.</returns>
    GitHubIssueEvent ProcessIssueWebhook(IssueWebhookPayload payload, string webhookId);
}
