namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for meta-cognitive capabilities (thinking about thinking)
/// </summary>
public interface IMetaCognitiveService
{
    Task<ThinkingProcess> StartThinkingAsync(string goal, CancellationToken ct);
    Task RecordThoughtAsync(Guid processId, Thought thought, CancellationToken ct);
    Task<ThinkingEvaluation> EvaluateThinkingAsync(Guid processId, CancellationToken ct);
    Task<ThinkingStrategy> AdjustStrategyAsync(Guid processId, ThinkingEvaluation evaluation, CancellationToken ct);
}

/// <summary>
/// Thinking process tracking
/// </summary>
public class ThinkingProcess
{
    public Guid Id { get; set; }
    public string Goal { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<Thought> Thoughts { get; set; } = new();
    public List<ThinkingStrategy> StrategyAdjustments { get; set; } = new();
}

/// <summary>
/// Single thought in the thinking process
/// </summary>
public class Thought
{
    public DateTime Timestamp { get; set; }
    public string Content { get; set; } = string.Empty;
    public ThoughtType Type { get; set; }
    public float Confidence { get; set; }
}

public enum ThoughtType
{
    Observation,
    Hypothesis,
    Decision,
    Reflection
}

/// <summary>
/// Evaluation of thinking process
/// </summary>
public class ThinkingEvaluation
{
    public float Efficiency { get; set; }
    public float Effectiveness { get; set; }
    public float ReasoningQuality { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Thinking strategy adjustment
/// </summary>
public class ThinkingStrategy
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime AppliedAt { get; set; }
}

