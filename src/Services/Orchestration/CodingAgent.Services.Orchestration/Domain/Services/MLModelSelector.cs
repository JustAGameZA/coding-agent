using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// ML-driven model selector that uses classification and performance metrics.
/// </summary>
public class MLModelSelector : IMLModelSelector
{
    private readonly IModelRegistry _modelRegistry;
    private readonly IModelPerformanceTracker _performanceTracker;
    private readonly IABTestingEngine _abTesting;
    private readonly ILogger<MLModelSelector> _logger;

    public MLModelSelector(
        IModelRegistry modelRegistry,
        IModelPerformanceTracker performanceTracker,
        IABTestingEngine abTesting,
        ILogger<MLModelSelector> logger)
    {
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _performanceTracker = performanceTracker ?? throw new ArgumentNullException(nameof(performanceTracker));
        _abTesting = abTesting ?? throw new ArgumentNullException(nameof(abTesting));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ModelSelectionResult> SelectBestModelAsync(
        string taskDescription,
        string taskType,
        TaskComplexity complexity,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Selecting best model for task: Type={TaskType}, Complexity={Complexity}",
            taskType, complexity);

        // Step 1: Check for active A/B test
        var activeTest = await _abTesting.GetActiveTestAsync(taskType, userId, cancellationToken);
        if (activeTest != null)
        {
            var requestId = Guid.NewGuid();
            var testVariant = await _abTesting.SelectVariantAsync(activeTest, requestId, cancellationToken);
            
            _logger.LogInformation(
                "A/B test {TestId} active: Selected variant {Variant}",
                activeTest.Id, testVariant);

            return new ModelSelectionResult
            {
                SelectedModel = testVariant,
                Reason = $"A/B test: {activeTest.Name}",
                Confidence = 0.5, // A/B test confidence is based on traffic allocation
                IsABTest = true,
                ABTestId = activeTest.Id
            };
        }

        // Step 2: Get available models
        var availableModels = await _modelRegistry.GetAvailableModelsAsync(cancellationToken);
        if (!availableModels.Any())
        {
            _logger.LogWarning("No models available, using default fallback");
            return new ModelSelectionResult
            {
                SelectedModel = "gpt-4o",
                Reason = "No models available, using default fallback",
                Confidence = 0.0
            };
        }

        // Step 3: Get performance-based recommendation
        var bestPerformingModel = await _performanceTracker.GetBestModelAsync(taskType, complexity, cancellationToken);
        
        if (bestPerformingModel != null && availableModels.Any(m => m.Name.Equals(bestPerformingModel, StringComparison.OrdinalIgnoreCase)))
        {
            var metrics = await _performanceTracker.GetMetricsAsync(bestPerformingModel, cancellationToken);
            
            _logger.LogInformation(
                "Performance-based selection: Model={Model}, SuccessRate={SuccessRate}, Executions={Executions}",
                bestPerformingModel, metrics?.SuccessRate ?? 0, metrics?.ExecutionCount ?? 0);

            return new ModelSelectionResult
            {
                SelectedModel = bestPerformingModel,
                Reason = $"Best performing model for {taskType}/{complexity} (Success rate: {metrics?.SuccessRate:P2})",
                Confidence = metrics?.SuccessRate ?? 0.5,
                AlternativeModels = availableModels
                    .Where(m => !m.Name.Equals(bestPerformingModel, StringComparison.OrdinalIgnoreCase))
                    .Take(3)
                    .Select(m => m.Name)
                    .ToList()
            };
        }

        // Step 4: Fallback to complexity-based selection
        var selectedModel = SelectModelByComplexity(complexity, availableModels);
        
        _logger.LogInformation(
            "Complexity-based selection: Model={Model}",
            selectedModel);

        return new ModelSelectionResult
        {
            SelectedModel = selectedModel,
            Reason = $"Complexity-based selection for {complexity}",
            Confidence = 0.6,
            AlternativeModels = availableModels
                .Where(m => !m.Name.Equals(selectedModel, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .Select(m => m.Name)
                .ToList()
        };
    }

    private string SelectModelByComplexity(TaskComplexity complexity, List<ModelInfo> availableModels)
    {
        // Try to find models that match complexity preferences
        var modelPreferences = complexity switch
        {
            TaskComplexity.Simple => new[] { "gpt-4o-mini", "mistral", "mistral:latest" },
            TaskComplexity.Medium => new[] { "gpt-4o", "qwen3-coder", "codellama:13b" },
            TaskComplexity.Complex => new[] { "gpt-4o", "qwen3-coder", "codellama:34b" },
            TaskComplexity.Epic => new[] { "gpt-4o", "codellama:34b", "deepseek-coder:33b" },
            _ => new[] { "gpt-4o" }
        };

        // Find first available model from preferences
        foreach (var preferred in modelPreferences)
        {
            var matching = availableModels.FirstOrDefault(m => 
                m.Name.Contains(preferred, StringComparison.OrdinalIgnoreCase));
            if (matching != null)
            {
                return matching.Name;
            }
        }

        // Fallback to first available model
        return availableModels.First().Name;
    }
}

