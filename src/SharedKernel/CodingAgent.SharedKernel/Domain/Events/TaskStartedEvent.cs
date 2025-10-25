using CodingAgent.SharedKernel.Domain.ValueObjects;

namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a task execution is started.
/// </summary>
public record TaskStartedEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the started task.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets the task type.
    /// </summary>
    public required TaskType TaskType { get; init; }

    /// <summary>
    /// Gets the task complexity.
    /// </summary>
    public required TaskComplexity Complexity { get; init; }

    /// <summary>
    /// Gets the execution strategy that will be used.
    /// </summary>
    public required ExecutionStrategy Strategy { get; init; }

    /// <summary>
    /// Gets the user identifier who owns the task.
    /// </summary>
    public required Guid UserId { get; init; }
}
