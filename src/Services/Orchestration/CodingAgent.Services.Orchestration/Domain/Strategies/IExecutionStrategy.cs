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
    Task<StrategyExecutionResult> ExecuteAsync(CodingTask task, TaskExecutionContext context, CancellationToken cancellationToken = default);
}
