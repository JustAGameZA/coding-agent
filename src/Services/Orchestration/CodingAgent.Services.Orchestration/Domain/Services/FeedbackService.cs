using System.Text.Json;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for collecting and analyzing feedback for continuous learning
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly IMemoryService? _memoryService;
    private readonly IMLTrainingClient? _mlTrainingClient;
    private readonly ILogger<FeedbackService> _logger;
    private readonly List<Feedback> _feedbacks = new(); // In-memory storage - replace with repository

    public FeedbackService(
        ILogger<FeedbackService> logger,
        IMemoryService? memoryService = null,
        IMLTrainingClient? mlTrainingClient = null)
    {
        _logger = logger;
        _memoryService = memoryService;
        _mlTrainingClient = mlTrainingClient;
    }

    public Task RecordFeedbackAsync(Feedback feedback, CancellationToken ct)
    {
        _logger.LogInformation("Recording feedback {FeedbackId} for task {TaskId}, type: {Type}", 
            feedback.Id, feedback.TaskId, feedback.Type);

        feedback.Id = Guid.NewGuid();
        feedback.CreatedAt = DateTime.UtcNow;
        _feedbacks.Add(feedback);

        // Update procedural memory based on feedback
        // Note: UpdateProcedureSuccessRateAsync is not yet available in IMemoryService
        // This will be implemented when Memory Service is fully integrated
        if (_memoryService != null && feedback.ProcedureId.HasValue)
        {
            try
            {
                // TODO: Implement UpdateProcedureSuccessRateAsync in IMemoryService when Memory Service is integrated
                // For now, we log the feedback but don't update procedural memory
                _logger.LogDebug(
                    "Feedback received for procedure {ProcedureId}, type: {Type}. " +
                    "Procedural memory update will be implemented when Memory Service is integrated.",
                    feedback.ProcedureId.Value,
                    feedback.Type);
                
                // Future implementation:
                // if (feedback.Type == FeedbackType.Positive)
                // {
                //     await _memoryService.UpdateProcedureSuccessRateAsync(
                //         feedback.ProcedureId.Value,
                //         true,
                //         TimeSpan.Zero,
                //         ct);
                // }
                // else if (feedback.Type == FeedbackType.Negative)
                // {
                //     await _memoryService.UpdateProcedureSuccessRateAsync(
                //         feedback.ProcedureId.Value,
                //         false,
                //         TimeSpan.Zero,
                //         ct);
                // }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process feedback for procedural memory");
            }
        }
        
        return Task.CompletedTask;
    }

    public Task<FeedbackAnalysis> AnalyzeFeedbackPatternsAsync(Guid taskId, CancellationToken ct)
    {
        _logger.LogInformation("Analyzing feedback patterns for task {TaskId}", taskId);

        var feedbacks = _feedbacks.Where(f => f.TaskId == taskId).ToList();

        if (!feedbacks.Any())
        {
            return Task.FromResult(new FeedbackAnalysis
            {
                TaskId = taskId,
                Patterns = new List<FeedbackPattern>(),
                Recommendations = new List<string>(),
                HasSignificantChanges = false
            });
        }

        // Simple pattern analysis - in production, use ML clustering
        var patterns = AnalyzePatterns(feedbacks);

        return Task.FromResult(new FeedbackAnalysis
        {
            TaskId = taskId,
            Patterns = patterns,
            Recommendations = GenerateRecommendations(patterns),
            HasSignificantChanges = patterns.Any(p => Math.Abs(p.NewSuccessRate - 0.5f) > 0.2f)
        });
    }

    public async Task UpdateModelParametersAsync(FeedbackAnalysis analysis, CancellationToken ct)
    {
        _logger.LogInformation("Updating model parameters based on feedback analysis for task {TaskId}", analysis.TaskId);

        foreach (var pattern in analysis.Patterns)
        {
            _logger.LogInformation("Pattern identified: {Description}, new success rate: {SuccessRate}", 
                pattern.PatternDescription, pattern.NewSuccessRate);
        }

        // Trigger ML model retraining if significant changes detected
        if (analysis.HasSignificantChanges && _mlTrainingClient != null)
        {
            try
            {
                _logger.LogInformation(
                    "Significant feedback changes detected for task {TaskId}, triggering ML model retraining",
                    analysis.TaskId);

                var retrainRequest = new TrainingRetrainRequest
                {
                    MinSamples = 1000, // Default minimum samples
                    ModelVersion = null // Let ML service generate version
                };

                var retrainResponse = await _mlTrainingClient.TriggerRetrainingAsync(retrainRequest, ct);

                _logger.LogInformation(
                    "ML model retraining triggered: Status={Status}, SamplesUsed={SamplesUsed}, " +
                    "NewModelVersion={NewModelVersion}, Accuracy={Accuracy}",
                    retrainResponse.Status,
                    retrainResponse.SamplesUsed,
                    retrainResponse.NewModelVersion,
                    retrainResponse.Accuracy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger ML model retraining for task {TaskId}", analysis.TaskId);
                // Don't throw - continue execution even if retraining fails
            }
        }
        else if (!analysis.HasSignificantChanges)
        {
            _logger.LogDebug("No significant changes detected, skipping ML model retraining");
        }
        else if (_mlTrainingClient == null)
        {
            _logger.LogWarning("ML Training client not available, cannot trigger retraining");
        }
    }

    private List<FeedbackPattern> AnalyzePatterns(List<Feedback> feedbacks)
    {
        // Simple pattern analysis
        var patterns = new List<FeedbackPattern>();

        // Group by procedure
        var byProcedure = feedbacks.Where(f => f.ProcedureId.HasValue)
            .GroupBy(f => f.ProcedureId!.Value);

        foreach (var group in byProcedure)
        {
            var positive = group.Count(f => f.Type == FeedbackType.Positive);
            var negative = group.Count(f => f.Type == FeedbackType.Negative);
            var total = group.Count();

            var successRate = total > 0 ? (float)positive / total : 0.5f;

            patterns.Add(new FeedbackPattern
            {
                ProcedureId = group.Key,
                NewSuccessRate = successRate,
                ImprovedSteps = new List<ProcedureStep>(), // Would be populated from analysis
                PatternDescription = $"Procedure {group.Key}: {positive} positive, {negative} negative out of {total} feedbacks"
            });
        }

        return patterns;
    }

    private List<string> GenerateRecommendations(List<FeedbackPattern> patterns)
    {
        var recommendations = new List<string>();

        foreach (var pattern in patterns)
        {
            if (pattern.NewSuccessRate < 0.5f)
            {
                recommendations.Add($"Procedure {pattern.ProcedureId} has low success rate ({pattern.NewSuccessRate:P0}). Consider reviewing and improving.");
            }
            else if (pattern.NewSuccessRate > 0.8f)
            {
                recommendations.Add($"Procedure {pattern.ProcedureId} has high success rate ({pattern.NewSuccessRate:P0}). Consider promoting to default strategy.");
            }
        }

        return recommendations;
    }
}

