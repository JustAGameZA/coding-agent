using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.SharedKernel.Domain.Entities;

namespace CodingAgent.Services.Orchestration.Domain.Entities;

/// <summary>
/// Represents an execution attempt of a coding task
/// </summary>
public class TaskExecution : BaseEntity
{
    public Guid TaskId { get; private set; }
    public ExecutionStrategy Strategy { get; private set; }
    public string ModelUsed { get; private set; } = string.Empty;
    public ExecutionStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Navigation property to the detailed result
    public ExecutionResult? Result { get; private set; }

    // EF Core constructor
    private TaskExecution() { }

    public TaskExecution(Guid taskId, ExecutionStrategy strategy, string modelUsed)
    {
        if (taskId == Guid.Empty)
        {
            throw new ArgumentException("Task ID cannot be empty", nameof(taskId));
        }

        if (string.IsNullOrWhiteSpace(modelUsed))
        {
            throw new ArgumentException("Model used cannot be empty", nameof(modelUsed));
        }

        TaskId = taskId;
        Strategy = strategy;
        ModelUsed = modelUsed;
        Status = ExecutionStatus.Pending;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(int tokensUsed, decimal costUSD, TimeSpan duration)
    {
        if (Status != ExecutionStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot complete execution in {Status} status");
        }

        if (tokensUsed < 0)
        {
            throw new ArgumentException("Tokens used cannot be negative", nameof(tokensUsed));
        }

        if (costUSD < 0)
        {
            throw new ArgumentException("Cost cannot be negative", nameof(costUSD));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentException("Duration cannot be negative", nameof(duration));
        }

        Status = ExecutionStatus.Success;
        CompletedAt = DateTime.UtcNow;

        // Create the detailed result entity
        Result = new ExecutionResult(Id, true, tokensUsed, costUSD);
        MarkAsUpdated();
    }

    public void Fail(string errorMessage)
    {
        if (Status != ExecutionStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot fail execution in {Status} status");
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));
        }

        ErrorMessage = errorMessage;
        Status = ExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;

        // Create the detailed result entity for failure
        Result = new ExecutionResult(Id, false, 0, 0);
        Result.SetError(errorMessage);
        MarkAsUpdated();
    }

    public void UpdateResult(ExecutionResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (result.ExecutionId != Id)
        {
            throw new ArgumentException("Result does not belong to this execution", nameof(result));
        }

        Result = result;
        MarkAsUpdated();
    }
}

public enum ExecutionStatus
{
    Pending,
    Success,
    Failed
}

