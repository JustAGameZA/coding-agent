using System.Diagnostics;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Repositories;
using CodingAgent.Services.Orchestration.Domain.Strategies;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.Services.Orchestration.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Coordinates the end-to-end execution of a task using a selected strategy.
/// Handles events, persistence, and log streaming.
/// </summary>
public class ExecutionCoordinator : IExecutionCoordinator
{
    private readonly IStrategySelector _strategySelector;
    private readonly IEnumerable<IExecutionStrategy> _strategies;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IExecutionLogService _logService;
    private readonly ILogger<ExecutionCoordinator> _logger;
    private readonly ActivitySource _activitySource;
    private readonly IReflectionService? _reflectionService; // Optional - reflection for agentic AI
    private readonly IPlanningService? _planningService; // Optional - planning for agentic AI

    public ExecutionCoordinator(
        IStrategySelector strategySelector,
        IEnumerable<IExecutionStrategy> strategies,
        IServiceScopeFactory scopeFactory,
        IExecutionLogService logService,
        ILogger<ExecutionCoordinator> logger,
        ActivitySource activitySource,
        IReflectionService? reflectionService = null,
        IPlanningService? planningService = null)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        _reflectionService = reflectionService;
        _planningService = planningService;
    }

    public async Task<TaskExecution> QueueExecutionAsync(CodingTask task, string? overrideStrategyName, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("QueueExecution");
        activity?.SetTag("task.id", task.Id);

        // Create scope for database operations
        using var scope = _scopeFactory.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var executionRepository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();

        // Agentic AI: Create plan for complex tasks if planning service is available
        Plan? plan = null;
        if (_planningService != null && 
            (task.Complexity == Domain.ValueObjects.TaskComplexity.High || 
             task.Description.Contains("multiple") || 
             task.Description.Contains("all")))
        {
            try
            {
                _logger.LogInformation("Creating plan for complex task {TaskId}", task.Id);
                plan = await _planningService.CreatePlanAsync(
                    task.Description,
                    task.Context ?? "",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create plan for task {TaskId}, falling back to direct execution", task.Id);
            }
        }

        // Select strategy (with optional override)
        var strategy = await _strategySelector.SelectStrategyAsync(task, overrideStrategyName, cancellationToken);

        // Start task and publish TaskStartedEvent
        await taskService.StartTaskAsync(task.Id, MapStrategyName(strategy.Name), cancellationToken);

        // Create TaskExecution record (model used is mapped by strategy)
        var modelUsed = MapModelUsed(strategy.Name);
        var execution = new TaskExecution(task.Id, MapStrategyName(strategy.Name), modelUsed);
        execution = await executionRepository.AddAsync(execution, cancellationToken);

        // Background execution - pass task ID and execution ID, not entities/repositories
        if (plan != null)
        {
            _ = Task.Run(() => ExecutePlanAsync(task.Id, plan, execution.Id, cancellationToken));
        }
        else
        {
            _ = Task.Run(() => ExecuteInternalAsync(task.Id, strategy.Name, execution.Id, cancellationToken));
        }

        return execution;
    }

    private async Task ExecuteInternalAsync(Guid taskId, string strategyName, Guid executionId, CancellationToken ct)
    {
        try
        {
            await _logService.WriteAsync(executionId, $"status:starting strategy={strategyName}", ct);

            // Create a new scope for this background task to get fresh DbContext
            using var scope = _scopeFactory.CreateScope();
            var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
            var executionRepository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

            // Load task from repository
            var task = await taskRepository.GetByIdAsync(taskId, ct) 
                ?? throw new InvalidOperationException($"Task {taskId} not found");

            // Get strategy instance
            var strategy = _strategies.FirstOrDefault(s => s.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Strategy {strategyName} not found");

            var context = new TaskExecutionContext();
            var sw = Stopwatch.StartNew();
            var result = await strategy.ExecuteAsync(task, context, ct);
            sw.Stop();

            var execution = await executionRepository.GetByIdAsync(executionId, ct) 
                ?? throw new InvalidOperationException("Execution not found");

            if (result.Success || result.HasPartialSuccess)
            {
                execution.Complete(result.TotalTokensUsed, result.TotalCostUSD, result.Duration);
                await executionRepository.UpdateAsync(execution, ct);

                await taskService.CompleteTaskAsync(
                    task.Id,
                    execution.Strategy,
                    result.TotalTokensUsed,
                    result.TotalCostUSD,
                    result.Duration,
                    ct);

                await _logService.WriteAsync(executionId, $"status:success tokens={result.TotalTokensUsed} cost={result.TotalCostUSD:F4} durationMs={result.Duration.TotalMilliseconds:F0}", ct);

                // Agentic AI: Reflect on execution if reflection service is available
                if (_reflectionService != null)
                {
                    try
                    {
                        var outcome = new ExecutionOutcome
                        {
                            Success = result.Success,
                            HasPartialSuccess = result.HasPartialSuccess,
                            Duration = result.Duration,
                            TokensUsed = result.TotalTokensUsed,
                            Errors = result.Errors ?? new List<string>(),
                            Results = new Dictionary<string, object>
                            {
                                ["changes"] = result.Changes?.Count ?? 0,
                                ["strategy"] = strategyName
                            }
                        };

                        var reflection = await _reflectionService.ReflectOnExecutionAsync(executionId, outcome, ct);
                        
                        // If reflection suggests improvements, generate plan
                        if (reflection.ConfidenceScore < 0.7 || reflection.ImprovementSuggestions.Any())
                        {
                            var improvementPlan = await _reflectionService.GenerateImprovementPlanAsync(reflection, ct);
                            _logger.LogInformation("Generated improvement plan for execution {ExecutionId} with {StepCount} steps", 
                                executionId, improvementPlan.Steps.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Reflection failed for execution {ExecutionId}", executionId);
                    }
                }
            }
            else
            {
                var error = result.Errors.FirstOrDefault() ?? "Unknown error";
                execution.Fail(error);
                await executionRepository.UpdateAsync(execution, ct);

                await taskService.FailTaskAsync(
                    task.Id,
                    execution.Strategy,
                    error,
                    result.TotalTokensUsed,
                    result.TotalCostUSD,
                    result.Duration,
                    ct);

                await _logService.WriteAsync(executionId, $"status:failed error={Escape(error)}", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution failed with exception for task {TaskId}", taskId);
            // Best-effort logging
            try { await _logService.WriteAsync(executionId, $"status:failed error={Escape(ex.Message)}", ct); } catch {}
        }
        finally
        {
            _logService.Complete(executionId);
        }
    }

    private async Task ExecutePlanAsync(Guid taskId, Plan plan, Guid executionId, CancellationToken ct)
    {
        try
        {
            await _logService.WriteAsync(executionId, $"status:planning plan_id={plan.Id} sub_tasks={plan.SubTasks.Count}", ct);

            foreach (var step in plan.SubTasks.OrderBy(s => GetStepOrder(s)))
            {
                // Wait for dependencies
                await WaitForDependenciesAsync(step, plan, ct);

                // Execute step
                var stepResult = await ExecuteStepAsync(taskId, step, ct);

                // Update plan progress
                if (_planningService != null)
                {
                    await _planningService.UpdatePlanProgressAsync(plan.Id, step, stepResult, ct);
                }

                // Validate step
                if (!ValidateStep(step, stepResult))
                {
                    // Re-plan if validation fails
                    if (_planningService != null)
                    {
                        plan = await _planningService.RefinePlanAsync(plan.Id, new ExecutionFeedback
                        {
                            StepFailed = step,
                            Reason = stepResult.Error ?? "Validation failed"
                        }, ct);
                        continue; // Retry with refined plan
                    }
                }

                if (!stepResult.Success)
                {
                    _logger.LogWarning("Step {StepId} failed: {Error}", step.Id, stepResult.Error);
                    // Continue to next step or re-plan based on strategy
                }
            }

            await _logService.WriteAsync(executionId, $"status:plan_completed plan_id={plan.Id}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan execution failed for task {TaskId}", taskId);
            await _logService.WriteAsync(executionId, $"status:plan_failed error={Escape(ex.Message)}", ct);
        }
    }

    private async Task WaitForDependenciesAsync(PlanStep step, Plan plan, CancellationToken ct)
    {
        foreach (var depId in step.Dependencies)
        {
            var depStep = plan.SubTasks.FirstOrDefault(s => s.Id == depId);
            if (depStep != null && depStep.Status != PlanStepStatus.Completed)
            {
                // Wait for dependency to complete (simplified - in production use proper async wait)
                await Task.Delay(100, ct);
            }
        }
    }

    private async Task<ExecutionResult> ExecuteStepAsync(Guid taskId, PlanStep step, CancellationToken ct)
    {
        // Simplified step execution - in production, this would invoke appropriate strategy
        await _logService.WriteAsync(taskId, $"executing:step {step.Id} {step.Description}", ct);
        
        // Use existing execution logic
        // For now, return success
        return new ExecutionResult
        {
            Success = true,
            Duration = TimeSpan.FromSeconds(1),
            Results = new Dictionary<string, object> { ["stepId"] = step.Id }
        };
    }

    private static bool ValidateStep(PlanStep step, ExecutionResult result)
    {
        // Simplified validation - check if step has validation criteria and result succeeded
        if (string.IsNullOrWhiteSpace(step.ValidationCriteria))
        {
            return result.Success;
        }

        // In production, evaluate validation criteria
        return result.Success;
    }

    private static int GetStepOrder(PlanStep step)
    {
        var match = System.Text.RegularExpressions.Regex.Match(step.Id, @"\d+");
        return match.Success ? int.Parse(match.Value) : int.MaxValue;
    }

    private static string Escape(string s) => s.Replace('\n', ' ').Replace('\r', ' ');

    private static Domain.ValueObjects.ExecutionStrategy MapStrategyName(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "singleshot" => ExecutionStrategy.SingleShot,
            "iterative" => ExecutionStrategy.Iterative,
            "multiagent" => ExecutionStrategy.MultiAgent,
            "hybridexecution" => ExecutionStrategy.HybridExecution,
            _ => ExecutionStrategy.Iterative
        };
    }

    private static string MapModelUsed(string strategyName)
    {
        return strategyName.ToLowerInvariant() switch
        {
            "singleshot" => "gpt-4o-mini",
            "iterative" => "gpt-4o",
            "multiagent" => "multiagent",
            "hybridexecution" => "hybrid",
            _ => strategyName
        };
    }
}
