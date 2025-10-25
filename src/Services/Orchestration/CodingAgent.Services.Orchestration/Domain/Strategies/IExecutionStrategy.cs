using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Strategies;

/// <summary>
/// Interface for execution strategy implementations.
/// Each strategy handles tasks of specific complexity levels.
/// </summary>
public interface IExecutionStrategy
{
    /// <summary>
    /// Gets the name of the strategy (e.g., "SingleShot", "Iterative").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the complexity level this strategy is designed for.
    /// </summary>
    ValueObjects.TaskComplexity SupportsComplexity { get; }

    /// <summary>
    /// Executes the strategy for the given task.
    /// </summary>
    /// <param name="task">The task to execute</param>
    /// <param name="context">Execution context with relevant files and metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result with changes and metrics</returns>
    Task<StrategyExecutionResult> ExecuteAsync(CodingTask task, TaskExecutionContext context, CancellationToken cancellationToken = default);
}
