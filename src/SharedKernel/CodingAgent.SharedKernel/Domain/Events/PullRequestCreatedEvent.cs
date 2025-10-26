namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a pull request is created by the coding agent.
/// </summary>
public record PullRequestCreatedEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the internal pull request ID.
    /// </summary>
    public required Guid PullRequestId { get; init; }

    /// <summary>
    /// Gets the GitHub pull request number.
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// Gets the repository owner.
    /// </summary>
    public required string RepositoryOwner { get; init; }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public required string RepositoryName { get; init; }

    /// <summary>
    /// Gets the pull request title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the pull request URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the head branch.
    /// </summary>
    public required string Head { get; init; }

    /// <summary>
    /// Gets the base branch.
    /// </summary>
    public required string Base { get; init; }

    /// <summary>
    /// Gets the pull request author.
    /// </summary>
    public required string Author { get; init; }
}
