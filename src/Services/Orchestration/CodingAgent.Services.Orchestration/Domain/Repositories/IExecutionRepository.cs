using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.SharedKernel.Abstractions;

namespace CodingAgent.Services.Orchestration.Domain.Repositories;

/// <summary>
/// Repository interface for TaskExecution entity
/// </summary>
public interface IExecutionRepository : IRepository<TaskExecution>
{
    /// <summary>
    /// Gets all executions for a specific task
    /// </summary>
    Task<IEnumerable<TaskExecution>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution with its detailed result
    /// </summary>
    Task<TaskExecution?> GetWithResultAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions by strategy type
    /// </summary>
    Task<IEnumerable<TaskExecution>> GetByStrategyAsync(
        ValueObjects.ExecutionStrategy strategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions by status
    /// </summary>
    Task<IEnumerable<TaskExecution>> GetByStatusAsync(
        ExecutionStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent execution for a task
    /// </summary>
    Task<TaskExecution?> GetLatestByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates total cost for a task's executions
    /// </summary>
    Task<decimal> GetTotalCostByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates total tokens used for a task's executions
    /// </summary>
    Task<int> GetTotalTokensByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
}
