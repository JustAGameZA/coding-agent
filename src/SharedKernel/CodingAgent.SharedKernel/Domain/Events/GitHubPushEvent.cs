namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a GitHub push webhook is received.
/// </summary>
public record GitHubPushEvent : IDomainEvent
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
    /// Gets the branch that was pushed to.
    /// </summary>
    public required string Branch { get; init; }

    /// <summary>
    /// Gets the commit SHA.
    /// </summary>
    public required string CommitSha { get; init; }

    /// <summary>
    /// Gets the commit message.
    /// </summary>
    public required string CommitMessage { get; init; }

    /// <summary>
    /// Gets the commit author.
    /// </summary>
    public required string CommitAuthor { get; init; }

    /// <summary>
    /// Gets the URL of the commit.
    /// </summary>
    public required string CommitUrl { get; init; }
}
