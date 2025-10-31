using CodingAgent.Services.Orchestration.Domain.Entities;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for goal decomposition and planning in agentic AI
/// </summary>
public interface IPlanningService
{
    Task<Plan> CreatePlanAsync(string goal, string context, CancellationToken ct);
    Task<Plan> RefinePlanAsync(Guid planId, ExecutionFeedback feedback, CancellationToken ct);
    Task<PlanStep> GetNextStepAsync(Guid planId, CancellationToken ct);
    Task UpdatePlanProgressAsync(Guid planId, PlanStep step, ExecutionResult result, CancellationToken ct);
}

/// <summary>
/// Hierarchical plan for goal execution
/// </summary>
public class Plan
{
    public Guid Id { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PlanStep> SubTasks { get; set; } = new();
    public string EstimatedTotalEffort { get; set; } = string.Empty;
    public List<string> Risks { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public PlanStatus Status { get; set; }
}

/// <summary>
/// Single step in a plan
/// </summary>
public class PlanStep
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public string EstimatedEffort { get; set; } = string.Empty; // "low", "medium", "high"
    public string ValidationCriteria { get; set; } = string.Empty;
    public List<PlanStep> SubSteps { get; set; } = new();
    public PlanStepStatus Status { get; set; }
    public ExecutionResult? Result { get; set; }
}

public enum PlanStatus
{
    Created,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum PlanStepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// Feedback for plan refinement
/// </summary>
public class ExecutionFeedback
{
    public PlanStep? StepFailed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Result of executing a plan step
/// </summary>
public class ExecutionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Results { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

