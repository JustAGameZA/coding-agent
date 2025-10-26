namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when an automated fix is attempted for a build failure.
/// </summary>
public record FixAttemptedEvent : IDomainEvent
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
    /// Gets the build ID that triggered the fix.
    /// </summary>
    public required Guid BuildId { get; init; }

    /// <summary>
    /// Gets the task ID created for the fix.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets the repository where the fix will be applied.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Gets the error message being fixed.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error pattern matched (if any).
    /// </summary>
    public string? ErrorPattern { get; init; }
}
