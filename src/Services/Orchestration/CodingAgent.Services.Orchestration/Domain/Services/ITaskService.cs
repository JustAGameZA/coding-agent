using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Interface for task management operations with event publishing.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Creates a new task and publishes TaskCreatedEvent.
    /// </summary>
    Task<CodingTask> CreateTaskAsync(
        Guid userId,
        string title,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing task's title and description.
    /// </summary>
    Task<CodingTask> UpdateTaskAsync(
        Guid taskId,
        string title,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a task.
    /// </summary>
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies a task with type and complexity.
    /// </summary>
    Task ClassifyTaskAsync(
        Guid taskId,
        TaskType type,
        TaskComplexity complexity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts task execution and publishes TaskStartedEvent.
    /// </summary>
    Task StartTaskAsync(
        Guid taskId,
        ExecutionStrategy strategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a task and publishes TaskCompletedEvent.
    /// </summary>
    Task CompleteTaskAsync(
        Guid taskId,
        ExecutionStrategy strategy,
        int tokensUsed,
        decimal costUsd,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a task as failed and publishes TaskFailedEvent.
    /// </summary>
    Task FailTaskAsync(
        Guid taskId,
        ExecutionStrategy strategy,
        string errorMessage,
        int tokensUsed,
        decimal costUsd,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    Task<CodingTask?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tasks for a user.
    /// </summary>
    Task<IEnumerable<CodingTask>> GetTasksByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
