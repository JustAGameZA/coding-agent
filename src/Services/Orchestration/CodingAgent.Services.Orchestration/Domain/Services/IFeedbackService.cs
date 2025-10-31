namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for collecting and analyzing feedback for continuous learning
/// </summary>
public interface IFeedbackService
{
    Task RecordFeedbackAsync(Feedback feedback, CancellationToken ct);
    Task<FeedbackAnalysis> AnalyzeFeedbackPatternsAsync(Guid taskId, CancellationToken ct);
    Task UpdateModelParametersAsync(FeedbackAnalysis analysis, CancellationToken ct);
}

/// <summary>
/// User or system feedback
/// </summary>
public class Feedback
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid? ExecutionId { get; set; }
    public Guid UserId { get; set; }
    public FeedbackType Type { get; set; }
    public float Rating { get; set; } // 0.0 to 1.0
    public string? Reason { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public Guid? ProcedureId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum FeedbackType
{
    Positive,
    Negative,
    Neutral
}

/// <summary>
/// Analysis of feedback patterns
/// </summary>
public class FeedbackAnalysis
{
    public Guid TaskId { get; set; }
    public List<FeedbackPattern> Patterns { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public bool HasSignificantChanges { get; set; }
}

/// <summary>
/// Identified pattern in feedback
/// </summary>
public class FeedbackPattern
{
    public Guid ProcedureId { get; set; }
    public float NewSuccessRate { get; set; }
    public List<ProcedureStep> ImprovedSteps { get; set; } = new();
    public string PatternDescription { get; set; } = string.Empty;
}

