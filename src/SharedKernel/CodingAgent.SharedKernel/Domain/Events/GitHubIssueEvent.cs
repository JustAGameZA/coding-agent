namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a GitHub issue webhook is received.
/// </summary>
public record GitHubIssueEvent : IDomainEvent
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
    /// Gets the issue action (opened, closed, commented, etc.).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the issue number.
    /// </summary>
    public required int IssueNumber { get; init; }

    /// <summary>
    /// Gets the issue title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the issue URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the author of the issue or comment.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Gets the comment body (only applicable when action is 'created' for comments).
    /// </summary>
    public string? CommentBody { get; init; }
}
