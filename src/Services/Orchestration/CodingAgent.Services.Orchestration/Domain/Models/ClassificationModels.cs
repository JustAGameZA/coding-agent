using CodingAgent.Services.Orchestration.Domain.ValueObjects;

namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Request model for ML Classifier service
/// </summary>
public class ClassificationRequest
{
    public string TaskDescription { get; set; } = string.Empty;
    public Dictionary<string, string>? Context { get; set; }
    public List<string>? FilesChanged { get; set; }
}

/// <summary>
/// Response model from ML Classifier service
/// </summary>
public class ClassificationResponse
{
    public string TaskType { get; set; } = string.Empty;
    public string Complexity { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public string SuggestedStrategy { get; set; } = string.Empty;
    public int EstimatedTokens { get; set; }
    public string? ClassifierUsed { get; set; }

    /// <summary>
    /// Parses the complexity string to TaskComplexity enum
    /// </summary>
    public TaskComplexity GetComplexity()
    {
        return Complexity?.ToLowerInvariant() switch
        {
            "simple" => TaskComplexity.Simple,
            "medium" => TaskComplexity.Medium,
            "complex" => TaskComplexity.Complex,
            "epic" => TaskComplexity.Epic,
            _ => TaskComplexity.Medium // default fallback
        };
    }
}
