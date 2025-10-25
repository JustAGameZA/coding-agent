using System.Diagnostics;
using System.Text.Json;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Strategies;

/// <summary>
/// MultiAgent execution strategy for complex tasks (200-1000 LOC changes).
/// Uses parallel specialized agents (Planner, Coder, Reviewer, Tester) for efficient execution.
/// </summary>
public class MultiAgentStrategy : IExecutionStrategy
{
    private readonly IPlannerAgent _plannerAgent;
    private readonly ICoderAgent _coderAgent;
    private readonly IReviewerAgent _reviewerAgent;
    private readonly ITesterAgent _testerAgent;
    private readonly ICodeValidator _validator;
    private readonly ILogger<MultiAgentStrategy> _logger;

    private const int MaxExecutionTimeSeconds = 180;

    public string Name => "MultiAgent";
    public TaskComplexity SupportsComplexity => TaskComplexity.Complex;

    public MultiAgentStrategy(
        IPlannerAgent plannerAgent,
        ICoderAgent coderAgent,
        IReviewerAgent reviewerAgent,
        ITesterAgent testerAgent,
        ICodeValidator validator,
        ILogger<MultiAgentStrategy> logger)
    {
        _plannerAgent = plannerAgent ?? throw new ArgumentNullException(nameof(plannerAgent));
        _coderAgent = coderAgent ?? throw new ArgumentNullException(nameof(coderAgent));
        _reviewerAgent = reviewerAgent ?? throw new ArgumentNullException(nameof(reviewerAgent));
        _testerAgent = testerAgent ?? throw new ArgumentNullException(nameof(testerAgent));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StrategyExecutionResult> ExecuteAsync(
        CodingTask task,
        TaskExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var stopwatch = Stopwatch.StartNew();
        var totalTokens = 0;
        var totalCost = 0m;
        var allResults = new List<AgentResult>();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(MaxExecutionTimeSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            _logger.LogInformation(
                "Starting MultiAgent execution for task {TaskId} (Type: {TaskType}, Complexity: {Complexity})",
                task.Id, task.Type, task.Complexity);

            // Phase 1: Planning
            _logger.LogInformation("MultiAgent Phase 1: Planning");
            var planResult = await _plannerAgent.CreatePlanAsync(task, context, linkedCts.Token);
            allResults.Add(planResult);
            totalTokens += planResult.TokensUsed;
            totalCost += planResult.Cost;

            if (!planResult.Success || string.IsNullOrWhiteSpace(planResult.Output))
            {
                return CreateFailureResult(
                    "Planning phase failed",
                    planResult.Errors,
                    totalTokens,
                    totalCost,
                    stopwatch.Elapsed);
            }

            var plan = JsonSerializer.Deserialize<TaskPlan>(
                planResult.Output,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (plan?.SubTasks == null || plan.SubTasks.Count == 0)
            {
                return CreateFailureResult(
                    "No subtasks generated in plan",
                    new List<string> { "Plan parsing produced no subtasks" },
                    totalTokens,
                    totalCost,
                    stopwatch.Elapsed);
            }

            _logger.LogInformation(
                "MultiAgent: Plan created with {SubTaskCount} subtasks. Strategy: {Strategy}",
                plan.SubTasks.Count, plan.Strategy);

            // Phase 2: Parallel Implementation
            _logger.LogInformation("MultiAgent Phase 2: Parallel Implementation");
            var coderTasks = plan.SubTasks
                .Where(st => st.Dependencies.Count == 0) // Execute independent subtasks first
                .Select(subTask => _coderAgent.ImplementSubTaskAsync(subTask, context, linkedCts.Token))
                .ToList();

            var coderResults = await Task.WhenAll(coderTasks);
            allResults.AddRange(coderResults);

            foreach (var result in coderResults)
            {
                totalTokens += result.TokensUsed;
                totalCost += result.Cost;
            }

            // Handle dependent subtasks sequentially
            var dependentSubTasks = plan.SubTasks.Where(st => st.Dependencies.Count > 0).ToList();
            if (dependentSubTasks.Any())
            {
                _logger.LogInformation(
                    "MultiAgent: Executing {DependentCount} dependent subtasks sequentially",
                    dependentSubTasks.Count);

                foreach (var subTask in dependentSubTasks)
                {
                    var result = await _coderAgent.ImplementSubTaskAsync(subTask, context, linkedCts.Token);
                    allResults.Add(result);
                    totalTokens += result.TokensUsed;
                    totalCost += result.Cost;
                }
            }

            // Aggregate all code changes (avoid double-counting coderResults already added to allResults)
            var allChanges = allResults
                .Where(r => r.AgentName.StartsWith("Coder-") && r.Success)
                .SelectMany(r => r.Changes)
                .ToList();

            if (allChanges.Count == 0)
            {
                return CreateFailureResult(
                    "No code changes generated by coder agents",
                    coderResults.SelectMany(r => r.Errors).ToList(),
                    totalTokens,
                    totalCost,
                    stopwatch.Elapsed);
            }

            _logger.LogInformation(
                "MultiAgent: Collected {ChangeCount} total code changes from all coders",
                allChanges.Count);

            // Phase 3: Conflict Resolution
            _logger.LogInformation("MultiAgent Phase 3: Conflict Resolution");
            var mergedChanges = ResolveConflicts(allChanges);

            _logger.LogInformation(
                "MultiAgent: After conflict resolution: {MergedCount} changes (from {OriginalCount})",
                mergedChanges.Count, allChanges.Count);

            // Phase 4: Code Review
            _logger.LogInformation("MultiAgent Phase 4: Code Review");
            var reviewResult = await _reviewerAgent.ReviewChangesAsync(mergedChanges, task, linkedCts.Token);
            allResults.Add(reviewResult);
            totalTokens += reviewResult.TokensUsed;
            totalCost += reviewResult.Cost;

            if (!reviewResult.Success)
            {
                return CreateFailureResult(
                    "Review phase failed",
                    reviewResult.Errors,
                    totalTokens,
                    totalCost,
                    stopwatch.Elapsed);
            }

            // Parse review result
            ReviewResult? review = null;
            if (!string.IsNullOrWhiteSpace(reviewResult.Output))
            {
                try
                {
                    review = JsonSerializer.Deserialize<ReviewResult>(
                        reviewResult.Output,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "MultiAgent: Failed to parse review result");
                }
            }

            // Check if review approved the changes
            if (review != null && !review.IsApproved && review.Severity >= 4)
            {
                _logger.LogWarning(
                    "MultiAgent: Review rejected changes with severity {Severity}",
                    review.Severity);

                return CreateFailureResult(
                    "Code review rejected changes",
                    review.Issues,
                    totalTokens,
                    totalCost,
                    stopwatch.Elapsed);
            }

            // Phase 5: Validation
            _logger.LogInformation("MultiAgent Phase 5: Validation");
            var validationResult = await _validator.ValidateAsync(mergedChanges, linkedCts.Token);

            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning(
                    "MultiAgent: Validation failed with {ErrorCount} errors",
                    validationResult.Errors.Count);

                return CreateFailureResult(
                    "Code validation failed",
                    validationResult.Errors.ToList(),
                    totalTokens,
                    totalCost,
                    stopwatch.Elapsed);
            }

            // Phase 6: Test Generation (best-effort, don't fail if it doesn't work)
            _logger.LogInformation("MultiAgent Phase 6: Test Generation");
            try
            {
                var testResult = await _testerAgent.GenerateTestsAsync(mergedChanges, task, linkedCts.Token);
                allResults.Add(testResult);
                totalTokens += testResult.TokensUsed;
                totalCost += testResult.Cost;

                if (testResult.Success && testResult.Changes.Any())
                {
                    // Add test files to the final changes with simple conflict resolution (last-write-wins)
                    var addedOrReplaced = 0;
                    foreach (var testChange in testResult.Changes)
                    {
                        var existingIndex = mergedChanges.FindIndex(c => c.FilePath == testChange.FilePath);
                        if (existingIndex >= 0)
                        {
                            mergedChanges[existingIndex] = testChange;
                        }
                        else
                        {
                            mergedChanges.Add(testChange);
                        }
                        addedOrReplaced++;
                    }

                    _logger.LogInformation(
                        "MultiAgent: Added {TestCount} test files (conflict resolution applied)",
                        addedOrReplaced);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MultiAgent: Test generation failed, continuing without tests");
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "MultiAgent execution completed successfully for task {TaskId}. " +
                "Files: {Files}, Duration: {Duration}ms, Tokens: {Tokens}, Cost: ${Cost:F4}",
                task.Id, mergedChanges.Count, stopwatch.ElapsedMilliseconds, totalTokens, totalCost);

            return StrategyExecutionResult.CreateSuccess(
                mergedChanges,
                totalTokens,
                totalCost,
                stopwatch.Elapsed,
                1);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MultiAgent execution timed out after {Timeout}s for task {TaskId}",
                MaxExecutionTimeSeconds, task.Id);

            return CreateFailureResult(
                $"Execution timed out after {MaxExecutionTimeSeconds} seconds",
                new List<string> { "Maximum execution time exceeded" },
                totalTokens,
                totalCost,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("MultiAgent execution cancelled for task {TaskId}", task.Id);

            return CreateFailureResult(
                "Execution was cancelled",
                new List<string> { "User requested cancellation" },
                totalTokens,
                totalCost,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "MultiAgent execution failed for task {TaskId}", task.Id);

            return CreateFailureResult(
                $"Unexpected error: {ex.Message}",
                new List<string> { ex.ToString() },
                totalTokens,
                totalCost,
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Resolves conflicts when multiple agents modify the same file.
    /// Uses last-write-wins strategy with logging for tracking.
    /// </summary>
    private List<CodeChange> ResolveConflicts(List<CodeChange> changes)
    {
        var fileGroups = changes.GroupBy(c => c.FilePath).ToList();

        var conflicts = fileGroups.Where(g => g.Count() > 1).ToList();
        if (conflicts.Any())
        {
            _logger.LogWarning(
                "MultiAgent: Found {ConflictCount} file conflicts to resolve",
                conflicts.Count);

            foreach (var conflict in conflicts)
            {
                _logger.LogWarning(
                    "MultiAgent: Conflict on file {FilePath} - {ChangeCount} changes. Using last change.",
                    conflict.Key, conflict.Count());
            }
        }

        // For conflicting files, take the last change (last-write-wins)
        var mergedChanges = fileGroups
            .Select(g => g.Last()) // Take the last change for each file
            .ToList();

        return mergedChanges;
    }

    private StrategyExecutionResult CreateFailureResult(
        string message,
        List<string> errors,
        int tokensUsed,
        decimal cost,
        TimeSpan duration)
    {
        return StrategyExecutionResult.CreateFailure(
            message,
            errors,
            tokensUsed,
            cost,
            duration,
            1);
    }
}
