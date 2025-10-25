using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Repositories;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;
using Microsoft.Extensions.Logging;
using SharedKernelTaskType = CodingAgent.SharedKernel.Domain.ValueObjects.TaskType;
using SharedKernelTaskComplexity = CodingAgent.SharedKernel.Domain.ValueObjects.TaskComplexity;
using SharedKernelExecutionStrategy = CodingAgent.SharedKernel.Domain.ValueObjects.ExecutionStrategy;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Domain service for managing coding tasks with event publishing.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        IEventPublisher eventPublisher,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CodingTask> CreateTaskAsync(
        Guid userId,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        var task = new CodingTask(userId, title, description);
        await _taskRepository.AddAsync(task, cancellationToken);

        // Publish TaskCreatedEvent
        await _eventPublisher.PublishAsync(new TaskCreatedEvent
        {
            TaskId = task.Id,
            UserId = userId,
            Description = description,
            TaskType = null, // Not yet classified
            Complexity = null // Not yet classified
        }, cancellationToken);

        _logger.LogInformation("Task {TaskId} created and event published", task.Id);

        return task;
    }

    public async Task<CodingTask> UpdateTaskAsync(
        Guid taskId,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task with ID {taskId} not found");

        task.UpdateDetails(title, description);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} updated", taskId);

        return task;
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task with ID {taskId} not found");

        // Don't allow deletion of tasks that are in progress
        if (task.Status == TaskStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot delete a task that is in progress");
        }

        await _taskRepository.DeleteAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} deleted", taskId);
    }

    public async Task ClassifyTaskAsync(
        Guid taskId,
        TaskType type,
        TaskComplexity complexity,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task with ID {taskId} not found");

        task.Classify(type, complexity);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} classified as {Type}/{Complexity}", taskId, type, complexity);
    }

    public async Task StartTaskAsync(
        Guid taskId,
        ExecutionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task with ID {taskId} not found");

        task.Start();
        await _taskRepository.UpdateAsync(task, cancellationToken);

        // Publish TaskStartedEvent with mapped values
        await _eventPublisher.PublishAsync(new TaskStartedEvent
        {
            TaskId = task.Id,
            TaskType = MapTaskType(task.Type),
            Complexity = MapTaskComplexity(task.Complexity),
            Strategy = MapExecutionStrategy(strategy),
            UserId = task.UserId
        }, cancellationToken);

        _logger.LogInformation("Task {TaskId} started and event published", taskId);
    }

    public async Task CompleteTaskAsync(
        Guid taskId,
        ExecutionStrategy strategy,
        int tokensUsed,
        decimal costUsd,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task with ID {taskId} not found");

        task.Complete();
        await _taskRepository.UpdateAsync(task, cancellationToken);

        // Publish TaskCompletedEvent with mapped values
        await _eventPublisher.PublishAsync(new TaskCompletedEvent
        {
            TaskId = task.Id,
            TaskType = MapTaskType(task.Type),
            Complexity = MapTaskComplexity(task.Complexity),
            Strategy = MapExecutionStrategy(strategy),
            Success = true,
            TokensUsed = tokensUsed,
            CostUsd = costUsd,
            Duration = duration,
            ErrorMessage = null
        }, cancellationToken);

        _logger.LogInformation("Task {TaskId} completed and event published", taskId);
    }

    public async Task FailTaskAsync(
        Guid taskId,
        ExecutionStrategy strategy,
        string errorMessage,
        int tokensUsed,
        decimal costUsd,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task with ID {taskId} not found");

        task.Fail(errorMessage);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        // Publish TaskFailedEvent with mapped values
        await _eventPublisher.PublishAsync(new TaskFailedEvent
        {
            TaskId = task.Id,
            TaskType = MapTaskType(task.Type),
            Complexity = MapTaskComplexity(task.Complexity),
            Strategy = MapExecutionStrategy(strategy),
            ErrorMessage = errorMessage,
            TokensUsed = tokensUsed,
            CostUsd = costUsd,
            Duration = duration
        }, cancellationToken);

        _logger.LogInformation("Task {TaskId} failed and event published", taskId);
    }

    public async Task<CodingTask?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _taskRepository.GetByIdAsync(taskId, cancellationToken);
    }

    public async Task<IEnumerable<CodingTask>> GetTasksByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _taskRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    // Mapping methods to convert between local and shared kernel enums
    private static SharedKernelTaskType MapTaskType(TaskType taskType)
    {
        return taskType switch
        {
            TaskType.BugFix => SharedKernelTaskType.BugFix,
            TaskType.Feature => SharedKernelTaskType.Feature,
            TaskType.Refactor => SharedKernelTaskType.Refactor,
            TaskType.Documentation => SharedKernelTaskType.Documentation,
            TaskType.Test => SharedKernelTaskType.Test,
            TaskType.Deployment => SharedKernelTaskType.Deployment,
            _ => throw new ArgumentOutOfRangeException(nameof(taskType), taskType, "Unknown task type")
        };
    }

    private static SharedKernelTaskComplexity MapTaskComplexity(TaskComplexity complexity)
    {
        return complexity switch
        {
            TaskComplexity.Simple => SharedKernelTaskComplexity.Simple,
            TaskComplexity.Medium => SharedKernelTaskComplexity.Medium,
            TaskComplexity.Complex => SharedKernelTaskComplexity.Complex,
            TaskComplexity.Epic => SharedKernelTaskComplexity.Epic,
            _ => throw new ArgumentOutOfRangeException(nameof(complexity), complexity, "Unknown complexity")
        };
    }

    private static SharedKernelExecutionStrategy MapExecutionStrategy(ExecutionStrategy strategy)
    {
        return strategy switch
        {
            ExecutionStrategy.SingleShot => SharedKernelExecutionStrategy.SingleShot,
            ExecutionStrategy.Iterative => SharedKernelExecutionStrategy.Iterative,
            ExecutionStrategy.MultiAgent => SharedKernelExecutionStrategy.MultiAgent,
            ExecutionStrategy.HybridExecution => SharedKernelExecutionStrategy.HybridExecution,
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown execution strategy")
        };
    }
}
