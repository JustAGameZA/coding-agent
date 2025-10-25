using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Planner agent that breaks down complex tasks into manageable subtasks.
/// Uses GPT-4o for strategic planning and task decomposition.
/// </summary>
public class PlannerAgent : IPlannerAgent
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<PlannerAgent> _logger;
    private const string ModelName = "gpt-4o";
    private const double Temperature = 0.3;
    private const int MaxTokens = 2000;

    public PlannerAgent(ILlmClient llmClient, ILogger<PlannerAgent> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> CreatePlanAsync(
        CodingTask task,
        TaskExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("PlannerAgent: Creating plan for task {TaskId}", task.Id);

            var prompt = BuildPlanningPrompt(task, context);
            
            var llmRequest = new LlmRequest
            {
                Model = ModelName,
                Messages = new List<LlmMessage>
                {
                    new() { Role = "system", Content = GetSystemPrompt() },
                    new() { Role = "user", Content = prompt }
                },
                Temperature = Temperature,
                MaxTokens = MaxTokens
            };

            var response = await _llmClient.GenerateAsync(llmRequest, cancellationToken);
            
            _logger.LogInformation(
                "PlannerAgent: LLM response received. Tokens: {Tokens}, Cost: ${Cost}",
                response.TokensUsed, response.Cost);

            var plan = ParsePlan(response.Content);
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "PlannerAgent: Created plan with {SubTaskCount} subtasks in {Duration}ms",
                plan.SubTasks.Count, stopwatch.ElapsedMilliseconds);

            return new AgentResult
            {
                AgentName = "Planner",
                Success = true,
                TokensUsed = response.TokensUsed,
                Cost = response.Cost,
                Duration = stopwatch.Elapsed,
                Output = JsonSerializer.Serialize(plan)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "PlannerAgent: Failed to create plan for task {TaskId}", task.Id);
            
            return new AgentResult
            {
                AgentName = "Planner",
                Success = false,
                Duration = stopwatch.Elapsed,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private string BuildPlanningPrompt(CodingTask task, TaskExecutionContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Task Planning Request");
        sb.AppendLine();
        sb.AppendLine($"**Task Title**: {task.Title}");
        sb.AppendLine($"**Description**: {task.Description}");
        sb.AppendLine($"**Type**: {task.Type}");
        sb.AppendLine($"**Complexity**: {task.Complexity}");
        sb.AppendLine();

        if (context.RelevantFiles.Any())
        {
            sb.AppendLine("## Relevant Files");
            foreach (var file in context.RelevantFiles)
            {
                sb.AppendLine($"- {file.Path}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Please analyze this task and create a detailed plan.");

        return sb.ToString();
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert software architect and planner. Your role is to break down complex coding tasks into manageable subtasks.

For the given task, create a plan with 2-5 subtasks. Each subtask should:
1. Be independently implementable
2. Have a clear scope (what files it affects)
3. Have explicit dependencies on other subtasks if any
4. Have an estimated complexity (1-10 scale)

Output your plan in the following JSON format:
```json
{
  ""subTasks"": [
    {
      ""id"": ""subtask-1"",
      ""title"": ""Brief title"",
      ""description"": ""Detailed description of what needs to be done"",
      ""affectedFiles"": [""path/to/file1.cs"", ""path/to/file2.cs""],
      ""estimatedComplexity"": 5,
      ""dependencies"": []
    }
  ],
  ""strategy"": ""Brief description of the overall strategy"",
  ""notes"": ""Any important considerations or risks""
}
```

Important:
- Keep subtasks focused and atomic
- Minimize dependencies between subtasks for parallel execution
- Consider file conflicts when defining subtasks
- Order subtasks logically (dependencies first)";
    }

    private TaskPlan ParsePlan(string content)
    {
        try
        {
            // Try to extract JSON from code blocks
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"```(?:json)?\s*(\{.*?\})\s*```", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            var jsonContent = jsonMatch.Success ? jsonMatch.Groups[1].Value : content;
            
            var planData = JsonSerializer.Deserialize<PlanJsonData>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (planData?.SubTasks == null || planData.SubTasks.Count == 0)
            {
                _logger.LogWarning("PlannerAgent: No subtasks found in response, creating default plan");
                return CreateDefaultPlan();
            }

            var subTasks = planData.SubTasks.Select(st => new SubTask
            {
                Id = st.Id ?? Guid.NewGuid().ToString(),
                Title = st.Title ?? "Untitled subtask",
                Description = st.Description ?? "",
                AffectedFiles = st.AffectedFiles ?? new List<string>(),
                EstimatedComplexity = st.EstimatedComplexity,
                Dependencies = st.Dependencies ?? new List<string>()
            }).ToList();

            return new TaskPlan
            {
                SubTasks = subTasks,
                Strategy = planData.Strategy ?? "Default strategy",
                Notes = planData.Notes
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "PlannerAgent: Failed to parse plan JSON, creating default plan");
            return CreateDefaultPlan();
        }
    }

    private TaskPlan CreateDefaultPlan()
    {
        return new TaskPlan
        {
            SubTasks = new List<SubTask>
            {
                new()
                {
                    Id = "default-1",
                    Title = "Implement task",
                    Description = "Implement the full task as a single unit",
                    AffectedFiles = new List<string>(),
                    EstimatedComplexity = 7
                }
            },
            Strategy = "Single subtask fallback",
            Notes = "Failed to parse LLM plan, using default single-subtask approach"
        };
    }

    // Internal class for JSON deserialization
    private class PlanJsonData
    {
        public List<SubTaskJsonData>? SubTasks { get; set; }
        public string? Strategy { get; set; }
        public string? Notes { get; set; }
    }

    private class SubTaskJsonData
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<string>? AffectedFiles { get; set; }
        public int EstimatedComplexity { get; set; }
        public List<string>? Dependencies { get; set; }
    }
}
