using CodingAgent.Services.Orchestration.Domain.Entities;

// Forward reference - Memory Service types will be referenced when integrated
// For now, using minimal interface to avoid circular dependencies

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for reflection and self-correction in agentic AI
/// </summary>
public interface IReflectionService
{
    Task<ReflectionResult> ReflectOnExecutionAsync(
        Guid executionId,
        ExecutionOutcome outcome,
        CancellationToken ct);
    
    Task<ImprovementPlan> GenerateImprovementPlanAsync(
        ReflectionResult reflection,
        CancellationToken ct);
}

/// <summary>
/// Result of reflection on execution
/// </summary>
public class ReflectionResult
{
    public Guid ExecutionId { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> KeyLessons { get; set; } = new();
    public List<string> ImprovementSuggestions { get; set; } = new();
    public float ConfidenceScore { get; set; }
    public Dictionary<string, object> ContextPattern { get; set; } = new();
}

/// <summary>
/// Minimal interface for Memory Service integration
/// Will be replaced with actual IMemoryService when Memory Service is integrated
/// </summary>
public interface IMemoryService
{
    Task<Episode> RecordEpisodeAsync(Episode episode, CancellationToken ct);
    Task<IEnumerable<Episode>> RetrieveSimilarEpisodesAsync(string query, int limit, CancellationToken ct);
    
    // Episode interface for Memory Service integration
    interface Episode
    {
        Guid? TaskId { get; }
        Guid? ExecutionId { get; }
        Guid UserId { get; }
        DateTime Timestamp { get; }
        string EventType { get; }
        Dictionary<string, object> Context { get; }
        Dictionary<string, object> Outcome { get; }
        List<string> LearnedPatterns { get; }
    }
}

/// <summary>
/// Improvement plan generated from reflection
/// </summary>
public class ImprovementPlan
{
    public Guid ReflectionId { get; set; }
    public List<ProcedureStep> Steps { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public float ExpectedImprovement { get; set; }
}

/// <summary>
/// Step in an improvement procedure
/// </summary>
public class ProcedureStep
{
    public ProcedureStep(int order, string description, Dictionary<string, object>? parameters = null, string? validationCriteria = null)
    {
        Order = order;
        Description = description;
        Parameters = parameters ?? new Dictionary<string, object>();
        ValidationCriteria = validationCriteria;
    }

    public int Order { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public string? ValidationCriteria { get; set; }
}

/// <summary>
/// Execution outcome for reflection
/// </summary>
public class ExecutionOutcome
{
    public bool Success { get; set; }
    public bool HasPartialSuccess { get; set; }
    public TimeSpan Duration { get; set; }
    public int TokensUsed { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
}

