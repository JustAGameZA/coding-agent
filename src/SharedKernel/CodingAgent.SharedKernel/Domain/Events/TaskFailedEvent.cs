using CodingAgent.SharedKernel.Domain.ValueObjects;

namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a task execution fails.
/// </summary>
public record TaskFailedEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the failed task.
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
    /// Gets the execution strategy used.
    /// </summary>
    public required ExecutionStrategy Strategy { get; init; }

    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the number of tokens used before failure.
    /// </summary>
    public int TokensUsed { get; init; }

    /// <summary>
    /// Gets the cost in USD incurred before failure.
    /// </summary>
    public decimal CostUsd { get; init; }

    /// <summary>
    /// Gets the duration before the failure occurred.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}
