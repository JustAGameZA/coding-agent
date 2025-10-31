namespace CodingAgent.Services.Memory.Domain.Entities;

/// <summary>
/// Represents procedural memory - learned strategies and heuristics for how to do things
/// </summary>
public class Procedure
{
    public Guid Id { get; private set; }
    public string ProcedureName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Dictionary<string, object> ContextPattern { get; private set; } = new(); // When to use this procedure
    public List<ProcedureStep> Steps { get; private set; } = new(); // Step-by-step instructions
    public float SuccessRate { get; private set; }
    public TimeSpan? AvgExecutionTime { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private Procedure() { }

    public Procedure(
        string procedureName,
        string description,
        Dictionary<string, object> contextPattern,
        List<ProcedureStep> steps)
    {
        Id = Guid.NewGuid();
        ProcedureName = procedureName;
        Description = description;
        ContextPattern = contextPattern;
        Steps = steps;
        SuccessRate = 0.0f;
        UsageCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordExecution(bool success, TimeSpan executionTime)
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;

        // Update success rate (exponential moving average)
        var alpha = 0.1f; // Smoothing factor
        SuccessRate = alpha * (success ? 1.0f : 0.0f) + (1 - alpha) * SuccessRate;

        // Update average execution time
        if (AvgExecutionTime == null)
        {
            AvgExecutionTime = executionTime;
        }
        else
        {
            AvgExecutionTime = TimeSpan.FromMilliseconds(
                alpha * executionTime.TotalMilliseconds + 
                (1 - alpha) * AvgExecutionTime.Value.TotalMilliseconds);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSteps(List<ProcedureStep> newSteps)
    {
        Steps = newSteps;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a single step in a procedure
/// </summary>
public class ProcedureStep
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? ValidationCriteria { get; set; }

    public ProcedureStep(int order, string description, Dictionary<string, object>? parameters = null, string? validationCriteria = null)
    {
        Order = order;
        Description = description;
        Parameters = parameters ?? new Dictionary<string, object>();
        ValidationCriteria = validationCriteria;
    }
}

