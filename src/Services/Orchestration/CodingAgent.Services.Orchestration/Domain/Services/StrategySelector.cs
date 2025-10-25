using System.Diagnostics;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Strategies;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Selects the appropriate execution strategy based on task complexity.
/// Integrates with ML Classifier service with fallback to heuristic classification.
/// </summary>
public class StrategySelector : IStrategySelector
{
    private readonly IMLClassifierClient _mlClassifierClient;
    private readonly IEnumerable<IExecutionStrategy> _strategies;
    private readonly ILogger<StrategySelector> _logger;
    private readonly ActivitySource _activitySource;

    public StrategySelector(
        IMLClassifierClient mlClassifierClient,
        IEnumerable<IExecutionStrategy> strategies,
        ILogger<StrategySelector> logger,
        ActivitySource activitySource)
    {
        _mlClassifierClient = mlClassifierClient ?? throw new ArgumentNullException(nameof(mlClassifierClient));
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));

        if (!_strategies.Any())
        {
            throw new ArgumentException("At least one execution strategy must be registered", nameof(strategies));
        }
    }

    public async Task<IExecutionStrategy> SelectStrategyAsync(
        CodingTask task,
        CancellationToken cancellationToken = default)
    {
        return await SelectStrategyAsync(task, null, cancellationToken);
    }

    public async Task<IExecutionStrategy> SelectStrategyAsync(
        CodingTask task,
        string? strategyName,
        CancellationToken cancellationToken = default)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        using var activity = _activitySource.StartActivity("SelectStrategy");
        activity?.SetTag("task.id", task.Id);
        activity?.SetTag("task.type", task.Type);
        activity?.SetTag("manual.override", !string.IsNullOrWhiteSpace(strategyName));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Manual override - return specific strategy if requested
            if (!string.IsNullOrWhiteSpace(strategyName))
            {
                _logger.LogInformation(
                    "Manual strategy override requested for task {TaskId}: {StrategyName}",
                    task.Id,
                    strategyName);

                var overrideStrategy = GetStrategyByName(strategyName);
                
                activity?.SetTag("strategy.selected", overrideStrategy.Name);
                activity?.SetTag("strategy.source", "manual");
                
                stopwatch.Stop();
                _logger.LogInformation(
                    "Strategy selected (manual): {Strategy} for task {TaskId} in {Duration}ms",
                    overrideStrategy.Name,
                    task.Id,
                    stopwatch.ElapsedMilliseconds);

                return overrideStrategy;
            }

            // Attempt ML classification
            TaskComplexity complexity;
            string classificationSource;

            try
            {
                _logger.LogInformation(
                    "Calling ML Classifier for task {TaskId}",
                    task.Id);

                var classificationRequest = new ClassificationRequest
                {
                    TaskDescription = task.Description
                };

                var classificationResponse = await _mlClassifierClient.ClassifyAsync(
                    classificationRequest,
                    cancellationToken);

                complexity = classificationResponse.GetComplexity();
                classificationSource = "ml";

                activity?.SetTag("classification.complexity", complexity);
                activity?.SetTag("classification.confidence", classificationResponse.Confidence);
                activity?.SetTag("classification.source", classificationSource);

                _logger.LogInformation(
                    "ML Classification for task {TaskId}: complexity={Complexity}, confidence={Confidence:F2}",
                    task.Id,
                    complexity,
                    classificationResponse.Confidence);

                // Update task with classification results only if not already started/completed
                if (task.Status == Domain.Entities.TaskStatus.Pending || task.Status == Domain.Entities.TaskStatus.Classifying)
                {
                    task.Classify(task.Type, complexity);
                }
            }
            catch (Exception ex)
            {
                // Fallback to heuristic classification
                _logger.LogWarning(
                    ex,
                    "ML Classifier unavailable for task {TaskId}, falling back to heuristic",
                    task.Id);

                complexity = ApplyHeuristicClassification(task);
                classificationSource = "heuristic";

                activity?.SetTag("classification.complexity", complexity);
                activity?.SetTag("classification.source", classificationSource);
                activity?.SetTag("classification.fallback", true);

                _logger.LogInformation(
                    "Heuristic classification for task {TaskId}: complexity={Complexity}",
                    task.Id,
                    complexity);

                // Update task with heuristic classification only if not already started/completed
                if (task.Status == Domain.Entities.TaskStatus.Pending || task.Status == Domain.Entities.TaskStatus.Classifying)
                {
                    task.Classify(task.Type, complexity);
                }
            }

            // Map complexity to strategy
            var strategy = MapComplexityToStrategy(complexity);

            activity?.SetTag("strategy.selected", strategy.Name);
            activity?.SetTag("strategy.source", classificationSource);

            stopwatch.Stop();

            _logger.LogInformation(
                "Strategy selected: {Strategy} for task {TaskId} (complexity={Complexity}, source={Source}, duration={Duration}ms)",
                strategy.Name,
                task.Id,
                complexity,
                classificationSource,
                stopwatch.ElapsedMilliseconds);

            // Ensure selection time is within target (log warning if exceeded)
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning(
                    "Strategy selection exceeded 100ms target: {Duration}ms for task {TaskId}",
                    stopwatch.ElapsedMilliseconds,
                    task.Id);
            }

            return strategy;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Strategy selection failed for task {TaskId} after {Duration}ms",
                task.Id,
                stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Maps task complexity to the appropriate execution strategy
    /// </summary>
    private IExecutionStrategy MapComplexityToStrategy(TaskComplexity complexity)
    {
        var strategyName = complexity switch
        {
            TaskComplexity.Simple => "SingleShot",
            TaskComplexity.Medium => "Iterative",
            TaskComplexity.Complex => "MultiAgent",
            TaskComplexity.Epic => "MultiAgent", // Epic also uses MultiAgent for now
            _ => "Iterative" // Default fallback
        };

        return GetStrategyByName(strategyName);
    }

    /// <summary>
    /// Retrieves a strategy by name from registered strategies
    /// </summary>
    private IExecutionStrategy GetStrategyByName(string name)
    {
        var strategy = _strategies.FirstOrDefault(s => 
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (strategy == null)
        {
            _logger.LogWarning(
                "Strategy {StrategyName} not found, falling back to Iterative",
                name);

            // Fallback to Iterative strategy
            strategy = _strategies.FirstOrDefault(s => 
                s.Name.Equals("Iterative", StringComparison.OrdinalIgnoreCase));

            if (strategy == null)
            {
                // Last resort - return first available strategy
                strategy = _strategies.First();
                _logger.LogWarning(
                    "Iterative strategy not found, using {StrategyName} as fallback",
                    strategy.Name);
            }
        }

        return strategy;
    }

    /// <summary>
    /// Applies simple heuristic classification based on description length and keywords
    /// </summary>
    private TaskComplexity ApplyHeuristicClassification(CodingTask task)
    {
        var description = task.Description?.ToLowerInvariant() ?? string.Empty;
        var wordCount = description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // Check for complexity indicators in description
        var complexIndicators = new[] { "architecture", "refactor", "rewrite", "migration", "complex" };
        var simpleIndicators = new[] { "fix", "typo", "small", "minor", "quick", "simple" };

        var hasComplexIndicator = complexIndicators.Any(indicator => description.Contains(indicator));
        var hasSimpleIndicator = simpleIndicators.Any(indicator => description.Contains(indicator));

        // Heuristic rules
        if (hasComplexIndicator || wordCount > 100)
        {
            return TaskComplexity.Complex;
        }
        else if (hasSimpleIndicator || wordCount < 20)
        {
            return TaskComplexity.Simple;
        }
        else
        {
            return TaskComplexity.Medium;
        }
    }
}
