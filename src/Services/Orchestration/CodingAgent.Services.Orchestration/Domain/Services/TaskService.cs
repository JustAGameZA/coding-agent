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
    private readonly IGitHubClient _githubClient;
    private readonly ILogger<TaskService> _logger;
    private const string EntityName = nameof(CodingTask);

    public TaskService(
        ITaskRepository taskRepository,
        IEventPublisher eventPublisher,
        IGitHubClient githubClient,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _githubClient = githubClient ?? throw new ArgumentNullException(nameof(githubClient));
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
            ?? throw new CodingAgent.SharedKernel.Exceptions.NotFoundException(EntityName, taskId);

        task.UpdateDetails(title, description);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} updated", taskId);

        return task;
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new CodingAgent.SharedKernel.Exceptions.NotFoundException(EntityName, taskId);

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
            ?? throw new CodingAgent.SharedKernel.Exceptions.NotFoundException(EntityName, taskId);

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
            ?? throw new CodingAgent.SharedKernel.Exceptions.NotFoundException(EntityName, taskId);

        // Only transition state for first execution
        // Pending or Classifying → InProgress
        // For re-execution of completed tasks, keep them in Completed state
        if (task.Status == TaskStatus.Pending)
        {
            // Manual override case - task wasn't classified, go straight to InProgress
            // Need to classify it first to satisfy state machine
            task.Classify(task.Type, TaskComplexity.Medium); // Default to Medium
            task.Start();
            await _taskRepository.UpdateAsync(task, cancellationToken);
        }
        else if (task.Status == TaskStatus.Classifying)
        {
            task.Start();
            await _taskRepository.UpdateAsync(task, cancellationToken);
        }
        // else: task is already InProgress, Completed, Failed, or Cancelled - don't change state

        // Publish TaskStartedEvent with mapped values (always publish for each execution)
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

        // Only transition state if currently InProgress (first execution completion)
        // For re-executions, task stays in Completed state
        if (task.Status == TaskStatus.InProgress)
        {
            task.Complete();
            await _taskRepository.UpdateAsync(task, cancellationToken);
        }

        // Publish TaskCompletedEvent with mapped values (always publish for each execution)
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

        // Create PR on GitHub if task was successful and has code changes
        // Only create PR on first completion (when PR not already created)
        if (task.PrNumber == null && task.Status == TaskStatus.Completed)
        {
            await TryCreatePullRequestAsync(task, cancellationToken);
        }
    }

    private async Task TryCreatePullRequestAsync(CodingTask task, CancellationToken cancellationToken)
    {
        try
        {
            // Check if GitHub service is available
            var isAvailable = await _githubClient.IsAvailableAsync(cancellationToken);
            if (!isAvailable)
            {
                _logger.LogWarning(
                    "GitHub service unavailable, skipping PR creation for task {TaskId}",
                    task.Id);
                return;
            }

            // TODO: Extract repo info from task context or configuration
            // For now, using placeholder values that should be configured
            var owner = "coding-agent"; // From config or task context
            var repo = "workspace";     // From config or task context
            var baseBranch = "main";    // From config or task context
            var headBranch = $"task/{task.Id}"; // Convention: task/{taskId}

            var prTitle = $"Task #{task.Id}: {task.Title}";
            var prBody = BuildPullRequestDescription(task);

            _logger.LogInformation(
                "Creating GitHub PR for task {TaskId}: {Owner}/{Repo} ({Head} -> {Base})",
                task.Id, owner, repo, headBranch, baseBranch);

            var pr = await _githubClient.CreatePullRequestAsync(
                owner,
                repo,
                prTitle,
                prBody,
                headBranch,
                baseBranch,
                isDraft: false,
                cancellationToken);

            // Store PR info on task
            task.SetPullRequest(pr.Number, pr.HtmlUrl);
            await _taskRepository.UpdateAsync(task, cancellationToken);

            // Publish PullRequestCreatedEvent
            await _eventPublisher.PublishAsync(new PullRequestCreatedEvent
            {
                PullRequestId = Guid.NewGuid(), // GitHub service will have its own ID
                Number = pr.Number,
                RepositoryOwner = owner,
                RepositoryName = repo,
                Title = prTitle,
                Url = pr.Url,
                Head = headBranch,
                Base = baseBranch,
                Author = "coding-agent" // TODO: Get from authentication context
            }, cancellationToken);

            _logger.LogInformation(
                "GitHub PR created for task {TaskId}: {Owner}/{Repo}#{Number} - {Url}",
                task.Id, owner, repo, pr.Number, pr.HtmlUrl);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the task - PR creation is optional
            _logger.LogError(
                ex,
                "Failed to create GitHub PR for task {TaskId}. Task completion succeeded but PR creation failed.",
                task.Id);
        }
    }

    private static string BuildPullRequestDescription(CodingTask task)
    {
        var description = $@"## Task Description

{task.Description}

## Task Details

- **Task ID**: {task.Id}
- **Type**: {task.Type}
- **Complexity**: {task.Complexity}
- **Created**: {task.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC
- **Completed**: {task.CompletedAt:yyyy-MM-dd HH:mm:ss} UTC

## Execution Summary

This pull request contains code changes generated by the Coding Agent for the task described above.

---
*Generated by Coding Agent v2.0*";

        return description;
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

        // Only transition state if currently InProgress (first execution failure)
        // For re-executions, task stays in its current state
        if (task.Status == TaskStatus.InProgress)
        {
            task.Fail(errorMessage);
            await _taskRepository.UpdateAsync(task, cancellationToken);
        }

        // Publish TaskFailedEvent with mapped values (always publish for each execution)
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
