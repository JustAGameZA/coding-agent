using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// In-memory performance tracker for model usage metrics.
/// In production, this would persist to a database.
/// </summary>
public class ModelPerformanceTracker : IModelPerformanceTracker
{
    private readonly ILogger<ModelPerformanceTracker> _logger;
    private readonly Dictionary<string, List<ModelExecutionResult>> _executionHistory = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(90); // Keep last 90 days

    public ModelPerformanceTracker(ILogger<ModelPerformanceTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordExecutionAsync(ModelExecutionResult result, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_executionHistory.ContainsKey(result.ModelName))
            {
                _executionHistory[result.ModelName] = new List<ModelExecutionResult>();
            }

            _executionHistory[result.ModelName].Add(result);

            // Clean up old records
            await CleanupOldRecordsAsync(cancellationToken);

            _logger.LogDebug(
                "Recorded execution for model {ModelName}: Success={Success}, Quality={Quality}, Duration={Duration}ms",
                result.ModelName, result.Success, result.QualityScore, result.Duration.TotalMilliseconds);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ModelPerformanceMetrics?> GetMetricsAsync(string modelName, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_executionHistory.TryGetValue(modelName, out var executions) || !executions.Any())
            {
                return null;
            }

            // Filter to recent executions (last 30 days for more relevant metrics)
            var recentExecutions = executions
                .Where(e => DateTime.UtcNow - e.ExecutedAt < TimeSpan.FromDays(30))
                .ToList();

            if (!recentExecutions.Any())
            {
                return null;
            }

            var metrics = new ModelPerformanceMetrics
            {
                ModelName = modelName,
                ExecutionCount = recentExecutions.Count,
                SuccessCount = recentExecutions.Count(e => e.Success),
                AverageQualityScore = recentExecutions
                    .Where(e => e.QualityScore.HasValue)
                    .Select(e => (double)e.QualityScore!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                AverageTokensUsed = recentExecutions.Average(e => e.TokensUsed),
                AverageCost = recentExecutions.Average(e => e.Cost),
                AverageDuration = TimeSpan.FromMilliseconds(recentExecutions.Average(e => e.Duration.TotalMilliseconds)),
                LastUpdated = DateTime.UtcNow
            };

            // Calculate success rates by task type
            var byTaskType = recentExecutions.GroupBy(e => e.TaskType);
            foreach (var group in byTaskType)
            {
                metrics.SuccessRateByTaskType[group.Key] = 
                    group.Count(e => e.Success) / (double)group.Count();
            }

            // Calculate success rates by complexity
            var byComplexity = recentExecutions.GroupBy(e => e.Complexity);
            foreach (var group in byComplexity)
            {
                metrics.SuccessRateByComplexity[group.Key] = 
                    group.Count(e => e.Success) / (double)group.Count();
            }

            return metrics;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Dictionary<string, ModelPerformanceMetrics>> GetAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var allModelNames = _executionHistory.Keys.ToList();
            var metrics = new Dictionary<string, ModelPerformanceMetrics>();

            foreach (var modelName in allModelNames)
            {
                var modelMetrics = await GetMetricsAsync(modelName, cancellationToken);
                if (modelMetrics != null)
                {
                    metrics[modelName] = modelMetrics;
                }
            }

            return metrics;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> GetBestModelAsync(string taskType, Domain.ValueObjects.TaskComplexity complexity, CancellationToken cancellationToken = default)
    {
        var allMetrics = await GetAllMetricsAsync(cancellationToken);

        if (!allMetrics.Any())
        {
            return null;
        }

        // Find model with best success rate for this task type and complexity
        string? bestModel = null;
        double bestScore = 0;

        foreach (var (modelName, metrics) in allMetrics)
        {
            // Calculate weighted score
            var taskTypeRate = metrics.SuccessRateByTaskType.GetValueOrDefault(taskType, metrics.SuccessRate);
            var complexityRate = metrics.SuccessRateByComplexity.GetValueOrDefault(complexity, metrics.SuccessRate);
            var overallRate = metrics.SuccessRate;
            var qualityScore = metrics.AverageQualityScore;

            // Weighted score: 40% task type, 30% complexity, 20% overall, 10% quality
            var score = (taskTypeRate * 0.4) + (complexityRate * 0.3) + (overallRate * 0.2) + (qualityScore / 10.0 * 0.1);

            // Require minimum 5 executions for reliable metrics
            if (metrics.ExecutionCount >= 5 && score > bestScore)
            {
                bestScore = score;
                bestModel = modelName;
            }
        }

        return bestModel;
    }

    private async Task CleanupOldRecordsAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow - _retentionPeriod;
        
        foreach (var (modelName, executions) in _executionHistory.ToList())
        {
            var filtered = executions.Where(e => e.ExecutedAt >= cutoffDate).ToList();
            
            if (filtered.Count != executions.Count)
            {
                _executionHistory[modelName] = filtered;
                _logger.LogDebug("Cleaned up {Count} old execution records for model {ModelName}", 
                    executions.Count - filtered.Count, modelName);
            }
        }

        await Task.CompletedTask;
    }
}

