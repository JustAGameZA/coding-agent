namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a CI/CD build fails.
/// </summary>
public record BuildFailedEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the failed build.
    /// </summary>
    public required Guid BuildId { get; init; }

    /// <summary>
    /// Gets the repository owner.
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Gets the branch name.
    /// </summary>
    public required string Branch { get; init; }

    /// <summary>
    /// Gets the commit SHA.
    /// </summary>
    public required string CommitSha { get; init; }

    /// <summary>
    /// Gets the workflow run ID from GitHub Actions.
    /// </summary>
    public required long WorkflowRunId { get; init; }

    /// <summary>
    /// Gets the workflow name.
    /// </summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// Gets the parsed error messages from the build logs.
    /// </summary>
    public required IReadOnlyList<string> ErrorMessages { get; init; }

    /// <summary>
    /// Gets the build conclusion (e.g., "failure", "cancelled").
    /// </summary>
    public required string Conclusion { get; init; }

    /// <summary>
    /// Gets the URL to the workflow run on GitHub.
    /// </summary>
    public required string WorkflowUrl { get; init; }

    /// <summary>
    /// Gets the timestamp when the build failed.
    /// </summary>
    public required DateTime FailedAt { get; init; }
}
