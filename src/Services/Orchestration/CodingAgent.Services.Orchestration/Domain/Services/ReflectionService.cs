using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using System.Text.Json;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for reflection and self-correction in agentic AI
/// </summary>
public class ReflectionService : IReflectionService
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ReflectionService> _logger;
    private readonly IMemoryService? _memoryService; // Optional - only if Memory Service is available

    public ReflectionService(
        ILlmClient llmClient,
        ILogger<ReflectionService> logger,
        IMemoryService? memoryService = null)
    {
        _llmClient = llmClient;
        _logger = logger;
        _memoryService = memoryService;
    }

    public async Task<ReflectionResult> ReflectOnExecutionAsync(
        Guid executionId,
        ExecutionOutcome outcome,
        CancellationToken ct)
    {
        _logger.LogInformation("Reflecting on execution {ExecutionId}", executionId);

        // Retrieve similar episodes from memory if available
        var similarEpisodes = new List<object>();
        if (_memoryService != null)
        {
            try
            {
                var episodes = await _memoryService.RetrieveSimilarEpisodesAsync(
                    $"execution_{executionId}", 5, ct);
                similarEpisodes = episodes.Select(e => new
                {
                    taskId = e.TaskId,
                    eventType = e.EventType,
                    outcome = e.Outcome
                }).Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve similar episodes from memory");
            }
        }

        // Build reflection prompt
        var reflectionPrompt = BuildReflectionPrompt(executionId, outcome, similarEpisodes);

        // Use LLM to critique execution
        var critique = await _llmClient.GenerateAsync(new LlmRequest
        {
            Model = "mistral:7b", // Use smaller model for reflection
            Messages = new List<LlmMessage>
            {
                new() { Role = "system", Content = "You are a reflective AI agent analyzing your own performance." },
                new() { Role = "user", Content = reflectionPrompt }
            },
            Temperature = 0.3, // Lower temperature for analytical tasks
            MaxTokens = 500
        }, ct);

        // Parse reflection results
        var reflection = ParseReflectionResult(executionId, critique.Content);

        // Store in episodic memory if available
        if (_memoryService != null)
        {
            try
            {
                // Use reflection helper to create episode
                var episode = CreateEpisodeFromReflection(executionId, reflection);
                await _memoryService.RecordEpisodeAsync(episode, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to store reflection in memory");
            }
        }

        return reflection;
    }

    public async Task<ImprovementPlan> GenerateImprovementPlanAsync(
        ReflectionResult reflection,
        CancellationToken ct)
    {
        _logger.LogInformation("Generating improvement plan for reflection {ReflectionId}", reflection.ExecutionId);

        var planPrompt = $@"
Based on this reflection, generate a concrete improvement plan:

REFLECTION:
Strengths: {string.Join(", ", reflection.Strengths)}
Weaknesses: {string.Join(", ", reflection.Weaknesses)}
Key Lessons: {string.Join(", ", reflection.KeyLessons)}
Suggestions: {string.Join(", ", reflection.ImprovementSuggestions)}

Create a step-by-step improvement plan in JSON format:
{{
    ""steps"": [
        {{
            ""order"": 1,
            ""description"": ""..."",
            ""parameters"": {{}},
            ""validation_criteria"": ""...""
        }}
    ],
    ""description"": ""..."",
    ""expected_improvement"": 0.0-1.0
}}";

        var planResponse = await _llmClient.GenerateAsync(new LlmRequest
        {
            Model = "mistral:7b",
            Messages = new List<LlmMessage>
            {
                new() { Role = "system", Content = "You are a planning AI agent creating improvement plans." },
                new() { Role = "user", Content = planPrompt }
            },
            Temperature = 0.2,
            MaxTokens = 1000
        }, ct);

        return ParseImprovementPlan(reflection.ExecutionId, planResponse.Content);
    }

    private string BuildReflectionPrompt(
        Guid executionId,
        ExecutionOutcome outcome,
        List<object> similarEpisodes)
    {
        var episodesSection = similarEpisodes.Any()
            ? $"\n\nSIMILAR PAST EXPERIENCES:\n{JsonSerializer.Serialize(similarEpisodes)}"
            : "";

        return $@"
You are a reflective AI agent analyzing your own performance.

EXECUTION ID: {executionId}
OUTCOME: {(outcome.Success ? "SUCCESS" : "FAILURE")}
PARTIAL SUCCESS: {outcome.HasPartialSuccess}
EXECUTION TIME: {outcome.Duration}
TOKENS USED: {outcome.TokensUsed}
ERRORS: {(outcome.Errors?.Any() == true ? string.Join(", ", outcome.Errors) : "None")}
RESULTS: {JsonSerializer.Serialize(outcome.Results)}
{episodesSection}

Please reflect on this execution:
1. What went well?
2. What could be improved?
3. What patterns do you notice compared to similar tasks?
4. What would you do differently next time?

Format your response as JSON:
{{
    ""strengths"": [""..."", ""...""],
    ""weaknesses"": [""..."", ""...""],
    ""key_lessons"": [""..."", ""...""],
    ""improvement_suggestions"": [""..."", ""...""],
    ""confidence_score"": 0.0-1.0
}}";
    }

    private ReflectionResult ParseReflectionResult(Guid executionId, string critique)
    {
        try
        {
            // Try to parse JSON response
            var json = ExtractJson(critique);
            var result = JsonSerializer.Deserialize<ReflectionResultJson>(json);
            
            if (result != null)
            {
                return new ReflectionResult
                {
                    ExecutionId = executionId,
                    Strengths = result.Strengths ?? new List<string>(),
                    Weaknesses = result.Weaknesses ?? new List<string>(),
                    KeyLessons = result.KeyLessons ?? new List<string>(),
                    ImprovementSuggestions = result.ImprovementSuggestions ?? new List<string>(),
                    ConfidenceScore = result.ConfidenceScore
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse reflection JSON, using fallback");
        }

        // Fallback: extract key information from text
        return new ReflectionResult
        {
            ExecutionId = executionId,
            Strengths = ExtractList(critique, "strengths"),
            Weaknesses = ExtractList(critique, "weaknesses"),
            KeyLessons = ExtractList(critique, "lessons"),
            ImprovementSuggestions = ExtractList(critique, "improvements"),
            ConfidenceScore = 0.5f // Default confidence
        };
    }

    private ImprovementPlan ParseImprovementPlan(Guid reflectionId, string planJson)
    {
        try
        {
            var json = ExtractJson(planJson);
            var result = JsonSerializer.Deserialize<ImprovementPlanJson>(json);
            
            if (result != null)
            {
                return new ImprovementPlan
                {
                    ReflectionId = reflectionId,
                    Steps = result.Steps?.Select(s => new ProcedureStep(
                        s.Order,
                        s.Description ?? "",
                        s.Parameters,
                        s.ValidationCriteria)).ToList() ?? new List<ProcedureStep>(),
                    Description = result.Description ?? "",
                    ExpectedImprovement = result.ExpectedImprovement
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse improvement plan JSON");
        }

        return new ImprovementPlan
        {
            ReflectionId = reflectionId,
            Steps = new List<ProcedureStep>(),
            Description = "Improvement plan generated from reflection",
            ExpectedImprovement = 0.3f
        };
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return text;
    }

    private static List<string> ExtractList(string text, string keyword)
    {
        // Simple extraction - look for patterns like "keyword: item1, item2"
        var pattern = $@"{keyword}["":\s]+([^\n]+)";
        var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        return new List<string>();
    }

    private static IMemoryService.Episode CreateEpisodeFromReflection(Guid executionId, ReflectionResult reflection)
    {
        // Helper method to create Episode from ReflectionResult
        // Note: This uses a simplified Episode type - will use actual Memory Service types when integrated
        // For now, we use the IMemoryService.Episode interface
        
        // Create episode data structure compatible with Memory Service
        // When Memory Service is integrated, use: new CodingAgent.Services.Memory.Domain.Entities.Episode(...)
        return new SimpleEpisode
        {
            TaskId = null,
            ExecutionId = executionId,
            UserId = Guid.Empty,
            Timestamp = DateTime.UtcNow,
            EventType = "reflection",
            Context = new Dictionary<string, object> { ["executionId"] = executionId },
            Outcome = new Dictionary<string, object>
            {
                ["strengths"] = reflection.Strengths,
                ["weaknesses"] = reflection.Weaknesses,
                ["keyLessons"] = reflection.KeyLessons,
                ["improvementSuggestions"] = reflection.ImprovementSuggestions,
                ["confidenceScore"] = reflection.ConfidenceScore
            },
            LearnedPatterns = reflection.KeyLessons
        };
    }
}

// Temporary Episode implementation for compilation
internal class SimpleEpisode : IMemoryService.Episode
{
    public Guid? TaskId { get; set; }
    public Guid? ExecutionId { get; set; }
    public Guid UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public Dictionary<string, object> Outcome { get; set; } = new();
    public List<string> LearnedPatterns { get; set; } = new();
}

// Temporary Episode class for compilation - will use actual Memory Service types when integrated
internal class Episode
{
    public Episode(Guid? taskId, Guid executionId, Guid userId, DateTime timestamp, string eventType, 
        Dictionary<string, object> context, Dictionary<string, object> outcome, List<string> learnedPatterns)
    {
        // Implementation
    }
}

// JSON deserialization models
internal class ReflectionResultJson
{
    public List<string>? Strengths { get; set; }
    public List<string>? Weaknesses { get; set; }
    public List<string>? KeyLessons { get; set; }
    public List<string>? ImprovementSuggestions { get; set; }
    public float ConfidenceScore { get; set; }
}

internal class ImprovementPlanJson
{
    public List<PlanStepJson>? Steps { get; set; }
    public string? Description { get; set; }
    public float ExpectedImprovement { get; set; }
}

internal class PlanStepJson
{
    public int Order { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string? ValidationCriteria { get; set; }
}

