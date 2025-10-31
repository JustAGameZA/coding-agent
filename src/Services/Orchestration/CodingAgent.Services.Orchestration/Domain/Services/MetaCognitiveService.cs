using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for meta-cognitive capabilities (thinking about thinking)
/// </summary>
public class MetaCognitiveService : IMetaCognitiveService
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<MetaCognitiveService> _logger;
    private readonly Dictionary<Guid, ThinkingProcess> _processes = new(); // In-memory storage

    public MetaCognitiveService(
        ILlmClient llmClient,
        ILogger<MetaCognitiveService> logger)
    {
        _llmClient = llmClient;
        _logger = logger;
    }

    public Task<ThinkingProcess> StartThinkingAsync(string goal, CancellationToken ct)
    {
        _logger.LogInformation("Starting thinking process for goal: {Goal}", goal);

        var process = new ThinkingProcess
        {
            Id = Guid.NewGuid(),
            Goal = goal,
            StartTime = DateTime.UtcNow,
            Thoughts = new List<Thought>(),
            StrategyAdjustments = new List<ThinkingStrategy>()
        };

        _processes[process.Id] = process;
        return Task.FromResult(process);
    }

    public Task RecordThoughtAsync(Guid processId, Thought thought, CancellationToken ct)
    {
        if (!_processes.TryGetValue(processId, out var process))
        {
            throw new InvalidOperationException($"Thinking process {processId} not found");
        }

        thought.Timestamp = DateTime.UtcNow;
        process.Thoughts.Add(thought);

        _logger.LogDebug("Recorded thought in process {ProcessId}: {Content}", processId, thought.Content);
        return Task.CompletedTask;
    }

    public async Task<ThinkingEvaluation> EvaluateThinkingAsync(Guid processId, CancellationToken ct)
    {
        _logger.LogInformation("Evaluating thinking process {ProcessId}", processId);

        if (!_processes.TryGetValue(processId, out var process))
        {
            throw new InvalidOperationException($"Thinking process {processId} not found");
        }

        // Analyze thinking patterns using LLM
        var analysisPrompt = BuildAnalysisPrompt(process);

        var response = await _llmClient.GenerateAsync(new LlmRequest
        {
            Model = "mistral:7b",
            Messages = new List<LlmMessage>
            {
                new() { Role = "system", Content = "You are a meta-cognitive AI evaluating thinking processes." },
                new() { Role = "user", Content = analysisPrompt }
            },
            Temperature = 0.2,
            MaxTokens = 500
        }, ct);

        // Parse evaluation
        var evaluation = ParseEvaluation(response.Content, process);

        return evaluation;
    }

    public Task<ThinkingStrategy> AdjustStrategyAsync(
        Guid processId,
        ThinkingEvaluation evaluation,
        CancellationToken ct)
    {
        if (!_processes.TryGetValue(processId, out var process))
        {
            throw new InvalidOperationException($"Thinking process {processId} not found");
        }

        // Generate strategy adjustment based on evaluation
        var strategy = new ThinkingStrategy
        {
            Name = "optimized_strategy",
            Description = "Strategy adjusted based on meta-cognitive evaluation",
            Parameters = new Dictionary<string, object>
            {
                ["efficiency_focus"] = evaluation.Efficiency < 0.5,
                ["effectiveness_focus"] = evaluation.Effectiveness < 0.5
            },
            AppliedAt = DateTime.UtcNow
        };

        process.StrategyAdjustments.Add(strategy);

        _logger.LogInformation("Adjusted strategy for process {ProcessId}", processId);
        return Task.FromResult(strategy);
    }

    private string BuildAnalysisPrompt(ThinkingProcess process)
    {
        var thoughtsSummary = string.Join("\n", process.Thoughts.Select(t => 
            $"- [{t.Type}] {t.Content} (confidence: {t.Confidence:F2})"));

        return $@"
Analyze this thinking process:

GOAL: {process.Goal}
DURATION: {DateTime.UtcNow - process.StartTime}
THOUGHTS ({process.Thoughts.Count}):
{thoughtsSummary}

Evaluate:
1. Efficiency (how quickly goal was achieved): 0.0-1.0
2. Effectiveness (quality of reasoning): 0.0-1.0
3. Reasoning quality: 0.0-1.0
4. Recommendations for improvement

Format as JSON:
{{
    ""efficiency"": 0.0-1.0,
    ""effectiveness"": 0.0-1.0,
    ""reasoning_quality"": 0.0-1.0,
    ""recommendations"": [""..."", ""...""]
}}";
    }

    private ThinkingEvaluation ParseEvaluation(string response, ThinkingProcess process)
    {
        try
        {
            // Try to parse JSON
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var result = System.Text.Json.JsonSerializer.Deserialize<EvaluationJson>(json);
                
                if (result != null)
                {
                    return new ThinkingEvaluation
                    {
                        Efficiency = result.Efficiency,
                        Effectiveness = result.Effectiveness,
                        ReasoningQuality = result.ReasoningQuality,
                        Recommendations = result.Recommendations ?? new List<string>()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse evaluation JSON");
        }

        // Fallback: calculate from process metrics
        var efficiency = process.EndTime.HasValue 
            ? Math.Min(1.0f, 1000.0f / (float)(process.EndTime.Value - process.StartTime).TotalSeconds)
            : 0.5f;
        
        var effectiveness = process.Thoughts.Any() 
            ? process.Thoughts.Average(t => t.Confidence)
            : 0.5f;

        return new ThinkingEvaluation
        {
            Efficiency = efficiency,
            Effectiveness = effectiveness,
            ReasoningQuality = effectiveness,
            Recommendations = new List<string> { "Continue monitoring thinking patterns" }
        };
    }
}

internal class EvaluationJson
{
    public float Efficiency { get; set; }
    public float Effectiveness { get; set; }
    public float ReasoningQuality { get; set; }
    public List<string>? Recommendations { get; set; }
}

