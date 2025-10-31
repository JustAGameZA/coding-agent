using System.Text.Json;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for collecting and analyzing feedback for continuous learning
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly IMemoryService? _memoryService;
    private readonly ILogger<FeedbackService> _logger;
    private readonly List<Feedback> _feedbacks = new(); // In-memory storage - replace with repository

    public FeedbackService(
        ILogger<FeedbackService> logger,
        IMemoryService? memoryService = null)
    {
        _logger = logger;
        _memoryService = memoryService;
    }

    public async Task RecordFeedbackAsync(Feedback feedback, CancellationToken ct)
    {
        _logger.LogInformation("Recording feedback {FeedbackId} for task {TaskId}, type: {Type}", 
            feedback.Id, feedback.TaskId, feedback.Type);

        feedback.Id = Guid.NewGuid();
        feedback.CreatedAt = DateTime.UtcNow;
        _feedbacks.Add(feedback);

        // Update procedural memory based on feedback
        if (_memoryService != null && feedback.ProcedureId.HasValue)
        {
            try
            {
                if (feedback.Type == FeedbackType.Positive)
                {
                    // Increase procedure confidence
                    await _memoryService.UpdateProcedureSuccessRateAsync(
                        feedback.ProcedureId.Value,
                        true,
                        TimeSpan.Zero, // Execution time not available from feedback
                        ct);
                }
                else if (feedback.Type == FeedbackType.Negative)
                {
                    // Decrease procedure confidence
                    await _memoryService.UpdateProcedureSuccessRateAsync(
                        feedback.ProcedureId.Value,
                        false,
                        TimeSpan.Zero,
                        ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update procedural memory from feedback");
            }
        }
    }

    public async Task<FeedbackAnalysis> AnalyzeFeedbackPatternsAsync(Guid taskId, CancellationToken ct)
    {
        _logger.LogInformation("Analyzing feedback patterns for task {TaskId}", taskId);

        var feedbacks = _feedbacks.Where(f => f.TaskId == taskId).ToList();

        if (!feedbacks.Any())
        {
            return new FeedbackAnalysis
            {
                TaskId = taskId,
                Patterns = new List<FeedbackPattern>(),
                Recommendations = new List<string>(),
                HasSignificantChanges = false
            };
        }

        // Simple pattern analysis - in production, use ML clustering
        var patterns = AnalyzePatterns(feedbacks);

        return new FeedbackAnalysis
        {
            TaskId = taskId,
            Patterns = patterns,
            Recommendations = GenerateRecommendations(patterns),
            HasSignificantChanges = patterns.Any(p => Math.Abs(p.NewSuccessRate - 0.5f) > 0.2f)
        };
    }

    public Task UpdateModelParametersAsync(FeedbackAnalysis analysis, CancellationToken ct)
    {
        _logger.LogInformation("Updating model parameters based on feedback analysis for task {TaskId}", analysis.TaskId);

        // In production, this would trigger ML model retraining
        // For now, just log the analysis
        foreach (var pattern in analysis.Patterns)
        {
            _logger.LogInformation("Pattern identified: {Description}, new success rate: {SuccessRate}", 
                pattern.PatternDescription, pattern.NewSuccessRate);
        }

        return Task.CompletedTask;
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

