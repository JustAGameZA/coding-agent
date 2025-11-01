using CodingAgent.Services.Orchestration.Domain.ValueObjects;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Tracks performance metrics for model usage to enable data-driven model selection.
/// </summary>
public interface IModelPerformanceTracker
{
    /// <summary>
    /// Records a model execution result for tracking.
    /// </summary>
    Task RecordExecutionAsync(ModelExecutionResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for a specific model.
    /// </summary>
    Task<ModelPerformanceMetrics?> GetMetricsAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for all models.
    /// </summary>
    Task<Dictionary<string, ModelPerformanceMetrics>> GetAllMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the best performing model for a specific task type and complexity.
    /// </summary>
    Task<string?> GetBestModelAsync(string taskType, Domain.ValueObjects.TaskComplexity complexity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a model execution for tracking.
/// </summary>
public class ModelExecutionResult
{
    public required string ModelName { get; set; }
    public required string TaskType { get; set; }
    public required Domain.ValueObjects.TaskComplexity Complexity { get; set; }
    public bool Success { get; set; }
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public TimeSpan Duration { get; set; }
    public int? QualityScore { get; set; } // 1-10 user rating or automatic quality score
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Performance metrics for a model.
/// </summary>
public class ModelPerformanceMetrics
{
    public required string ModelName { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public double SuccessRate => ExecutionCount > 0 ? (double)SuccessCount / ExecutionCount : 0;
    public double AverageQualityScore { get; set; }
    public double AverageTokensUsed { get; set; }
    public decimal AverageCost { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public Dictionary<string, double> SuccessRateByTaskType { get; set; } = new();
    public Dictionary<Domain.ValueObjects.TaskComplexity, double> SuccessRateByComplexity { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

