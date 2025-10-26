namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when an automated fix successfully fixes a build failure.
/// </summary>
public record FixSucceededEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier for the fix attempt.
    /// </summary>
    public required Guid FixAttemptId { get; init; }

    /// <summary>
    /// Gets the task ID that generated the fix.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets the pull request number created with the fix.
    /// </summary>
    public required int PullRequestNumber { get; init; }

    /// <summary>
    /// Gets the pull request URL.
    /// </summary>
    public required string PullRequestUrl { get; init; }

    /// <summary>
    /// Gets the repository where the fix was applied.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Gets the error pattern that was fixed.
    /// </summary>
    public string? ErrorPattern { get; init; }
}
