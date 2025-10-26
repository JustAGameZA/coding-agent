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
    /// Gets the unique identifier for the build.
    /// </summary>
    public required Guid BuildId { get; init; }

    /// <summary>
    /// Gets the repository owner.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Gets the branch where the build failed.
    /// </summary>
    public required string Branch { get; init; }

    /// <summary>
    /// Gets the commit SHA that triggered the build.
    /// </summary>
    public required string CommitSha { get; init; }

    /// <summary>
    /// Gets the error message from the build failure.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the full build error log.
    /// </summary>
    public string? ErrorLog { get; init; }

    /// <summary>
    /// Gets the workflow name.
    /// </summary>
    public string? WorkflowName { get; init; }

    /// <summary>
    /// Gets the job name that failed.
    /// </summary>
    public string? JobName { get; init; }
}
