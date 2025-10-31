using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using System.Text.Json;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for goal decomposition and planning in agentic AI
/// </summary>
public class PlanningService : IPlanningService
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<PlanningService> _logger;
    private readonly IMemoryService? _memoryService; // Optional
    private readonly Dictionary<Guid, Plan> _activePlans = new();

    public PlanningService(
        ILlmClient llmClient,
        ILogger<PlanningService> logger,
        IMemoryService? memoryService = null)
    {
        _llmClient = llmClient;
        _logger = logger;
        _memoryService = memoryService;
    }

    public async Task<Plan> CreatePlanAsync(string goal, string context, CancellationToken ct)
    {
        _logger.LogInformation("Creating plan for goal: {Goal}", goal);

        // 1. Retrieve relevant memories
        var relevantEpisodes = new List<object>();
        var relevantProcedures = new List<object>();
        
        if (_memoryService != null)
        {
            try
            {
                var episodes = await _memoryService.RetrieveSimilarEpisodesAsync(goal, 10, ct);
                relevantEpisodes = episodes.Select(e => new
                {
                    taskId = e.TaskId,
                    eventType = e.EventType,
                    outcome = e.Outcome
                }).Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve episodes from memory");
            }
        }

        // 2. Generate decomposition prompt
        var planningPrompt = BuildPlanningPrompt(goal, context, relevantEpisodes, relevantProcedures);

        // 3. Use LLM to generate plan
        var response = await _llmClient.GenerateAsync(new LlmRequest
        {
            Model = "mistral:7b",
            Messages = new List<LlmMessage>
            {
                new() { Role = "system", Content = "You are an AI planning agent. Create detailed execution plans." },
                new() { Role = "user", Content = planningPrompt }
            },
            Temperature = 0.2, // Lower temperature for structured planning
            MaxTokens = 2000
        }, ct);

        // 4. Parse and validate plan
        var plan = ParsePlan(response.Content, goal, context);
        plan = ValidateAndOptimizePlan(plan);

        // 5. Store plan
        plan.Id = Guid.NewGuid();
        plan.CreatedAt = DateTime.UtcNow;
        plan.Status = PlanStatus.Created;
        _activePlans[plan.Id] = plan;

        _logger.LogInformation("Created plan {PlanId} with {SubTaskCount} sub-tasks", plan.Id, plan.SubTasks.Count);
        return plan;
    }

    public async Task<Plan> RefinePlanAsync(Guid planId, ExecutionFeedback feedback, CancellationToken ct)
    {
        _logger.LogInformation("Refining plan {PlanId} based on feedback", planId);

        if (!_activePlans.TryGetValue(planId, out var plan))
        {
            throw new InvalidOperationException($"Plan {planId} not found");
        }

        // Build refinement prompt
        var refinementPrompt = BuildRefinementPrompt(plan, feedback);

        // Use LLM to refine plan
        var response = await _llmClient.GenerateAsync(new LlmRequest
        {
            Model = "mistral:7b",
            Messages = new List<LlmMessage>
            {
                new() { Role = "system", Content = "You are an AI planning agent refining a plan based on feedback." },
                new() { Role = "user", Content = refinementPrompt }
            },
            Temperature = 0.3,
            MaxTokens = 2000
        }, ct);

        // Parse refined plan
        var refinedPlan = ParsePlan(response.Content, plan.Goal, "");
        refinedPlan.Id = planId;
        refinedPlan.Status = PlanStatus.InProgress;

        // Merge with existing plan (preserve completed steps)
        foreach (var existingStep in plan.SubTasks.Where(s => s.Status == PlanStepStatus.Completed))
        {
            var refinedStep = refinedPlan.SubTasks.FirstOrDefault(s => s.Id == existingStep.Id);
            if (refinedStep != null)
            {
                refinedStep.Status = PlanStepStatus.Completed;
                refinedStep.Result = existingStep.Result;
            }
        }

        _activePlans[planId] = refinedPlan;
        return refinedPlan;
    }

    public Task<PlanStep> GetNextStepAsync(Guid planId, CancellationToken ct)
    {
        if (!_activePlans.TryGetValue(planId, out var plan))
        {
            throw new InvalidOperationException($"Plan {planId} not found");
        }

        // Find next step that has all dependencies completed
        var nextStep = plan.SubTasks
            .Where(s => s.Status == PlanStepStatus.Pending)
            .Where(s => s.Dependencies.All(dep => 
                plan.SubTasks.FirstOrDefault(st => st.Id == dep)?.Status == PlanStepStatus.Completed))
            .OrderBy(s => GetStepOrder(s))
            .FirstOrDefault();

        if (nextStep == null)
        {
            throw new InvalidOperationException("No next step available");
        }

        nextStep.Status = PlanStepStatus.InProgress;
        return Task.FromResult(nextStep);
    }

    public Task UpdatePlanProgressAsync(Guid planId, PlanStep step, ExecutionResult result, CancellationToken ct)
    {
        if (!_activePlans.TryGetValue(planId, out var plan))
        {
            throw new InvalidOperationException($"Plan {planId} not found");
        }

        var planStep = plan.SubTasks.FirstOrDefault(s => s.Id == step.Id);
        if (planStep != null)
        {
            planStep.Result = result;
            planStep.Status = result.Success ? PlanStepStatus.Completed : PlanStepStatus.Failed;
        }

        // Update plan status
        if (plan.SubTasks.All(s => s.Status == PlanStepStatus.Completed || s.Status == PlanStepStatus.Skipped))
        {
            plan.Status = PlanStatus.Completed;
        }
        else if (plan.SubTasks.Any(s => s.Status == PlanStepStatus.Failed))
        {
            plan.Status = PlanStatus.Failed;
        }

        return Task.CompletedTask;
    }

    private string BuildPlanningPrompt(
        string goal,
        string context,
        List<object> relevantEpisodes,
        List<object> relevantProcedures)
    {
        var episodesSection = relevantEpisodes.Any()
            ? $"\n\nRELEVANT PAST EXPERIENCES:\n{JsonSerializer.Serialize(relevantEpisodes)}"
            : "";

        var proceduresSection = relevantProcedures.Any()
            ? $"\n\nAVAILABLE PROCEDURES:\n{JsonSerializer.Serialize(relevantProcedures)}"
            : "";

        return $@"
You are an AI planning agent. Your task is to create a detailed execution plan.

GOAL: {goal}
CONTEXT: {context}
{episodesSection}
{proceduresSection}

Create a hierarchical plan that:
1. Breaks the goal into sub-tasks
2. Sequences sub-tasks optimally
3. Identifies dependencies between sub-tasks
4. Estimates effort for each sub-task
5. Includes validation steps

Format as JSON:
{{
    ""goal"": ""..."",
    ""description"": ""..."",
    ""sub_tasks"": [
        {{
            ""id"": ""step_1"",
            ""description"": ""..."",
            ""dependencies"": [],
            ""estimated_effort"": ""low|medium|high"",
            ""validation_criteria"": ""..."",
            ""sub_steps"": []
        }}
    ],
    ""estimated_total_effort"": ""..."",
    ""risks"": [""..."", ""...""]
}}";
    }

    private string BuildRefinementPrompt(Plan plan, ExecutionFeedback feedback)
    {
        return $@"
You are refining an existing plan based on feedback.

CURRENT PLAN:
{JsonSerializer.Serialize(plan)}

FEEDBACK:
Step Failed: {feedback.StepFailed?.Description ?? "None"}
Reason: {feedback.Reason}
Context: {JsonSerializer.Serialize(feedback.Context)}

Refine the plan to address the feedback while preserving completed steps.
Return the complete refined plan in the same JSON format.";
    }

    private Plan ParsePlan(string planJson, string goal, string context)
    {
        try
        {
            var json = ExtractJson(planJson);
            var result = JsonSerializer.Deserialize<PlanJson>(json);
            
            if (result != null)
            {
                return new Plan
                {
                    Goal = result.Goal ?? goal,
                    Description = result.Description ?? "",
                    SubTasks = result.SubTasks?.Select(s => new PlanStep
                    {
                        Id = s.Id ?? Guid.NewGuid().ToString(),
                        Description = s.Description ?? "",
                        Dependencies = s.Dependencies ?? new List<string>(),
                        EstimatedEffort = s.EstimatedEffort ?? "medium",
                        ValidationCriteria = s.ValidationCriteria ?? "",
                        SubSteps = s.SubSteps?.Select(ss => new PlanStep
                        {
                            Id = ss.Id ?? Guid.NewGuid().ToString(),
                            Description = ss.Description ?? "",
                            Dependencies = ss.Dependencies ?? new List<string>(),
                            EstimatedEffort = ss.EstimatedEffort ?? "low",
                            ValidationCriteria = ss.ValidationCriteria ?? ""
                        }).ToList() ?? new List<PlanStep>(),
                        Status = PlanStepStatus.Pending
                    }).ToList() ?? new List<PlanStep>(),
                    EstimatedTotalEffort = result.EstimatedTotalEffort ?? "medium",
                    Risks = result.Risks ?? new List<string>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse plan JSON, using fallback");
        }

        // Fallback: create simple plan
        return new Plan
        {
            Goal = goal,
            Description = $"Plan for: {goal}",
            SubTasks = new List<PlanStep>
            {
                new PlanStep
                {
                    Id = "step_1",
                    Description = goal,
                    Dependencies = new List<string>(),
                    EstimatedEffort = "medium",
                    Status = PlanStepStatus.Pending
                }
            },
            EstimatedTotalEffort = "medium",
            Risks = new List<string>()
        };
    }

    private Plan ValidateAndOptimizePlan(Plan plan)
    {
        // Validate plan structure
        if (!plan.SubTasks.Any())
        {
            _logger.LogWarning("Plan has no sub-tasks, adding default step");
            plan.SubTasks.Add(new PlanStep
            {
                Id = "step_1",
                Description = plan.Goal,
                Status = PlanStepStatus.Pending
            });
        }

        // Ensure all dependencies reference valid steps
        var stepIds = plan.SubTasks.Select(s => s.Id).ToHashSet();
        foreach (var step in plan.SubTasks)
        {
            step.Dependencies = step.Dependencies.Where(d => stepIds.Contains(d)).ToList();
        }

        return plan;
    }

    private static int GetStepOrder(PlanStep step)
    {
        // Extract numeric order from step ID (e.g., "step_1" -> 1)
        var match = System.Text.RegularExpressions.Regex.Match(step.Id, @"\d+");
        return match.Success ? int.Parse(match.Value) : int.MaxValue;
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
}

// JSON deserialization models
internal class PlanJson
{
    public string? Goal { get; set; }
    public string? Description { get; set; }
    public List<PlanStepJson>? SubTasks { get; set; }
    public string? EstimatedTotalEffort { get; set; }
    public List<string>? Risks { get; set; }
}

internal class PlanStepJson
{
    public string? Id { get; set; }
    public string? Description { get; set; }
    public List<string>? Dependencies { get; set; }
    public string? EstimatedEffort { get; set; }
    public string? ValidationCriteria { get; set; }
    public List<PlanStepJson>? SubSteps { get; set; }
}

