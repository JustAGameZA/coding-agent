using System.Diagnostics;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Repositories;
using CodingAgent.Services.Orchestration.Domain.Strategies;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.Services.Orchestration.Infrastructure.Logging;
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
    private readonly ITaskService _taskService;
    private readonly IExecutionRepository _executionRepository;
    private readonly IExecutionLogService _logService;
    private readonly ILogger<ExecutionCoordinator> _logger;
    private readonly ActivitySource _activitySource;

    public ExecutionCoordinator(
        IStrategySelector strategySelector,
        IEnumerable<IExecutionStrategy> strategies,
        ITaskService taskService,
        IExecutionRepository executionRepository,
        IExecutionLogService logService,
        ILogger<ExecutionCoordinator> logger,
        ActivitySource activitySource)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    public async Task<TaskExecution> QueueExecutionAsync(CodingTask task, string? overrideStrategyName, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("QueueExecution");
        activity?.SetTag("task.id", task.Id);

        // Select strategy (with optional override)
        var strategy = await _strategySelector.SelectStrategyAsync(task, overrideStrategyName, cancellationToken);

        // Start task and publish TaskStartedEvent
        await _taskService.StartTaskAsync(task.Id, MapStrategyName(strategy.Name), cancellationToken);

        // Create TaskExecution record (model used is mapped by strategy)
        var modelUsed = MapModelUsed(strategy.Name);
        var execution = new TaskExecution(task.Id, MapStrategyName(strategy.Name), modelUsed);
        execution = await _executionRepository.AddAsync(execution, cancellationToken);

        // Background execution
        _ = Task.Run(() => ExecuteInternalAsync(task, strategy, execution.Id, cancellationToken));

        return execution;
    }

    private async Task ExecuteInternalAsync(CodingTask task, IExecutionStrategy strategy, Guid executionId, CancellationToken ct)
    {
        try
        {
            await _logService.WriteAsync(executionId, $"status:starting strategy={strategy.Name}", ct);

            var context = new TaskExecutionContext();
            var sw = Stopwatch.StartNew();
            var result = await strategy.ExecuteAsync(task, context, ct);
            sw.Stop();

            var execution = await _executionRepository.GetByIdAsync(executionId, ct) 
                ?? throw new InvalidOperationException("Execution not found");

            if (result.Success)
            {
                execution.Complete(result.TotalTokensUsed, result.TotalCostUSD, result.Duration);
                await _executionRepository.UpdateAsync(execution, ct);

                await _taskService.CompleteTaskAsync(
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
                await _executionRepository.UpdateAsync(execution, ct);

                await _taskService.FailTaskAsync(
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
            _logger.LogError(ex, "Execution failed with exception for task {TaskId}", task.Id);
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
