namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Client interface for ML Classifier training endpoints.
/// Handles submission of feedback and triggering model retraining.
/// </summary>
public interface IMLTrainingClient
{
    /// <summary>
    /// Submits classification feedback to the ML service for model improvement.
    /// </summary>
    /// <param name="request">Training feedback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SubmitFeedbackAsync(TrainingFeedbackRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers model retraining in the ML service.
    /// </summary>
    /// <param name="request">Retraining request with minimum samples</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training response with status and new model version</returns>
    Task<TrainingRetrainResponse> TriggerRetrainingAsync(TrainingRetrainRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training statistics from the ML service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training statistics</returns>
    Task<TrainingStatsResponse> GetTrainingStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for submitting training feedback.
/// </summary>
public class TrainingFeedbackRequest
{
    public required string TaskDescription { get; set; }
    public required string PredictedType { get; set; }
    public required string PredictedComplexity { get; set; }
    public string? ActualType { get; set; }
    public string? ActualComplexity { get; set; }
    public required double Confidence { get; set; }
    public required string ClassifierUsed { get; set; }
    public bool? WasCorrect { get; set; }
}

/// <summary>
/// Request model for triggering model retraining.
/// </summary>
public class TrainingRetrainRequest
{
    public int MinSamples { get; set; } = 1000;
    public string? ModelVersion { get; set; }
}

/// <summary>
/// Response model from retraining request.
/// </summary>
public class TrainingRetrainResponse
{
    public required string Status { get; set; }
    public required string Message { get; set; }
    public int? SamplesUsed { get; set; }
    public string? NewModelVersion { get; set; }
    public double? Accuracy { get; set; }
}

/// <summary>
/// Response model for training statistics.
/// </summary>
public class TrainingStatsResponse
{
    public int TotalSamples { get; set; }
    public int SamplesWithFeedback { get; set; }
    public Dictionary<string, int> Distribution { get; set; } = new();
    public double AverageConfidence { get; set; }
    public double? Accuracy { get; set; }
}

