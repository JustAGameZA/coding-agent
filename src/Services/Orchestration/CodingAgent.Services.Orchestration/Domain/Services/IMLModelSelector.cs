using CodingAgent.Services.Orchestration.Domain.ValueObjects;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Selects the best model for a task using ML classification and performance metrics.
/// </summary>
public interface IMLModelSelector
{
    /// <summary>
    /// Selects the best model for a task based on classification and performance data.
    /// </summary>
    Task<ModelSelectionResult> SelectBestModelAsync(
        string taskDescription,
        string taskType,
        TaskComplexity complexity,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of model selection.
/// </summary>
public class ModelSelectionResult
{
    public required string SelectedModel { get; set; }
    public string? Reason { get; set; }
    public double Confidence { get; set; }
    public bool IsABTest { get; set; }
    public Guid? ABTestId { get; set; }
    public List<string> AlternativeModels { get; set; } = new();
}

