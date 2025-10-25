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

    public ExecutionCoordinator(
        IStrategySelector strategySelector,
        IEnumerable<IExecutionStrategy> strategies,
        IServiceScopeFactory scopeFactory,
        IExecutionLogService logService,
        ILogger<ExecutionCoordinator> logger,
        ActivitySource activitySource)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    public async Task<TaskExecution> QueueExecutionAsync(CodingTask task, string? overrideStrategyName, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("QueueExecution");
        activity?.SetTag("task.id", task.Id);

        // Create scope for database operations
        using var scope = _scopeFactory.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var executionRepository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();

        // Select strategy (with optional override)
        var strategy = await _strategySelector.SelectStrategyAsync(task, overrideStrategyName, cancellationToken);

        // Start task and publish TaskStartedEvent
        await taskService.StartTaskAsync(task.Id, MapStrategyName(strategy.Name), cancellationToken);

        // Create TaskExecution record (model used is mapped by strategy)
        var modelUsed = MapModelUsed(strategy.Name);
        var execution = new TaskExecution(task.Id, MapStrategyName(strategy.Name), modelUsed);
        execution = await executionRepository.AddAsync(execution, cancellationToken);

        // Background execution - pass task ID and execution ID, not entities/repositories
        _ = Task.Run(() => ExecuteInternalAsync(task.Id, strategy.Name, execution.Id, cancellationToken));

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

            if (result.Success)
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
