namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a GitHub pull request webhook is received.
/// </summary>
public record GitHubPullRequestEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the GitHub webhook delivery ID.
    /// </summary>
    public required string WebhookId { get; init; }

    /// <summary>
    /// Gets the repository owner.
    /// </summary>
    public required string RepositoryOwner { get; init; }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public required string RepositoryName { get; init; }

    /// <summary>
    /// Gets the pull request action (opened, closed, etc.).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the pull request number.
    /// </summary>
    public required int PullRequestNumber { get; init; }

    /// <summary>
    /// Gets the pull request title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the pull request URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the author of the pull request.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Gets whether the pull request was merged (only applicable when action is 'closed').
    /// </summary>
    public bool? Merged { get; init; }
}
