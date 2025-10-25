using CodingAgent.Services.Orchestration.Domain.Entities;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Coordinates task execution lifecycle: strategy selection, execution, persistence, events, and logging.
/// </summary>
public interface IExecutionCoordinator
{
    /// <summary>
    /// Queues execution for a task using the selected strategy (or override) and returns the created execution entity.
    /// Execution runs in the background; use SSE logs or executions endpoint to monitor progress.
    /// </summary>
    /// <param name="task">The task to execute</param>
    /// <param name="overrideStrategyName">Optional strategy name override (e.g., "SingleShot")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created TaskExecution record</returns>
    Task<TaskExecution> QueueExecutionAsync(CodingTask task, string? overrideStrategyName, CancellationToken cancellationToken = default);
}
