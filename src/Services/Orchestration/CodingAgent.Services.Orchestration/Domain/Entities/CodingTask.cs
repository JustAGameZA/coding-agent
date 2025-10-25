using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.SharedKernel.Domain.Entities;

namespace CodingAgent.Services.Orchestration.Domain.Entities;

/// <summary>
/// Represents a coding task to be executed
/// </summary>
public class CodingTask : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskType Type { get; private set; }
    public TaskComplexity Complexity { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<TaskExecution> _executions = new();
    public IReadOnlyCollection<TaskExecution> Executions => _executions.AsReadOnly();

    // EF Core constructor
    private CodingTask() { }

    public CodingTask(Guid userId, string title, string description)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty", nameof(description));
        }

        UserId = userId;
        Title = title;
        Description = description;
        Status = TaskStatus.Pending;
    }

    public void Classify(TaskType type, TaskComplexity complexity)
    {
        ValidateStatusTransition(TaskStatus.Classifying);
        Type = type;
        Complexity = complexity;
        Status = TaskStatus.Classifying;
        MarkAsUpdated();
    }

    public void Start()
    {
        ValidateStatusTransition(TaskStatus.InProgress);
        Status = TaskStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Complete()
    {
        ValidateStatusTransition(TaskStatus.Completed);
        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Fail(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));
        }

        ValidateStatusTransition(TaskStatus.Failed);
        Status = TaskStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Cancel()
    {
        ValidateStatusTransition(TaskStatus.Cancelled);
        Status = TaskStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Validates if the status transition is allowed based on the current state
    /// </summary>
    private void ValidateStatusTransition(TaskStatus newStatus)
    {
        var isValidTransition = (Status, newStatus) switch
        {
            // From Pending
            (TaskStatus.Pending, TaskStatus.Classifying) => true,
            (TaskStatus.Pending, TaskStatus.Cancelled) => true,

            // From Classifying
            (TaskStatus.Classifying, TaskStatus.InProgress) => true,
            (TaskStatus.Classifying, TaskStatus.Failed) => true,
            (TaskStatus.Classifying, TaskStatus.Cancelled) => true,

            // From InProgress
            (TaskStatus.InProgress, TaskStatus.Completed) => true,
            (TaskStatus.InProgress, TaskStatus.Failed) => true,
            (TaskStatus.InProgress, TaskStatus.Cancelled) => true,

            // Terminal states cannot transition
            (TaskStatus.Completed, _) => false,
            (TaskStatus.Failed, _) => false,
            (TaskStatus.Cancelled, _) => false,

            // All other transitions are invalid
            _ => false
        };

        if (!isValidTransition)
        {
            throw new InvalidOperationException(
                $"Invalid status transition from {Status} to {newStatus}");
        }
    }

    public void AddExecution(TaskExecution execution)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        if (execution.TaskId != Id)
        {
            throw new ArgumentException("Execution does not belong to this task", nameof(execution));
        }

        _executions.Add(execution);
        MarkAsUpdated();
    }
}

public enum TaskStatus
{
    Pending,
    Classifying,
    InProgress,
    Completed,
    Failed,
    Cancelled
}
