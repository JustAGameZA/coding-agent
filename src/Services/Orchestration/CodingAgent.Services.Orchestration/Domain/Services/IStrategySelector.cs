using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Strategies;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for selecting the appropriate execution strategy based on task complexity.
/// Integrates with ML Classifier service for intelligent strategy selection.
/// </summary>
public interface IStrategySelector
{
    /// <summary>
    /// Selects the optimal execution strategy for the given task.
    /// Uses ML classification with fallback to heuristic analysis.
    /// </summary>
    /// <param name="task">The task to select a strategy for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The selected execution strategy</returns>
    Task<IExecutionStrategy> SelectStrategyAsync(CodingTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects an execution strategy with manual override capability.
    /// If strategyName is provided, returns that strategy regardless of classification.
    /// </summary>
    /// <param name="task">The task to select a strategy for</param>
    /// <param name="strategyName">Optional strategy name override (e.g., "SingleShot", "Iterative")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The selected execution strategy</returns>
    Task<IExecutionStrategy> SelectStrategyAsync(CodingTask task, string? strategyName, CancellationToken cancellationToken = default);
}
