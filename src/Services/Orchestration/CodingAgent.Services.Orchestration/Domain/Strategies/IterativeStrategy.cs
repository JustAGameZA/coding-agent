using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Strategies;

/// <summary>
/// Iterative execution strategy for medium-complexity tasks (50-200 LOC).
/// Uses multi-turn conversation with validation feedback to refine solutions.
/// </summary>
public class IterativeStrategy : IExecutionStrategy
{
    private const int MaxIterations = 3;
    private const int TimeoutSeconds = 60;
    private const string ModelName = "gpt-4o";
    private const int MaxTokensPerRequest = 4000;
    private const double DefaultTemperature = 0.3;
    
    // Precompiled regex patterns for performance and to prevent catastrophic backtracking
    private static readonly Regex FilePatternRegex = new(
        @"FILE:\s*(.+)",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));
    
    private static readonly Regex CodeBlockPatternRegex = new(
        @"```(\w+)?\s*\n(.*?)\n```",
        RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromSeconds(2));

    private readonly ILlmClient _llmClient;
    private readonly ICodeValidator _validator;
    private readonly ILogger<IterativeStrategy> _logger;

    public string Name => "Iterative";
    public TaskComplexity SupportsComplexity => TaskComplexity.Medium;

    public IterativeStrategy(
        ILlmClient llmClient,
        ICodeValidator validator,
        ILogger<IterativeStrategy> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
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
        var conversationHistory = new List<LlmMessage>();
        var totalTokens = 0;
        var totalCost = 0m;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            _logger.LogInformation(
                "Starting iterative execution for task {TaskId} (Type: {TaskType}, Complexity: {Complexity})",
                task.Id, task.Type, task.Complexity);

            // Add system prompt
            conversationHistory.Add(new LlmMessage
            {
                Role = "system",
                Content = GetSystemPrompt()
            });

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                context.Iteration = iteration;

                _logger.LogInformation(
                    "Iteration {Iteration}/{MaxIterations} for task {TaskId}",
                    iteration + 1, MaxIterations, task.Id);

                // Build user prompt for current iteration
                var userPrompt = BuildUserPrompt(task, context, iteration);
                conversationHistory.Add(new LlmMessage
                {
                    Role = "user",
                    Content = userPrompt
                });

                // Generate solution from LLM
                var llmRequest = new LlmRequest
                {
                    Model = ModelName,
                    Messages = conversationHistory,
                    Temperature = DefaultTemperature,
                    MaxTokens = MaxTokensPerRequest
                };

                LlmResponse llmResponse;
                try
                {
                    llmResponse = await _llmClient.GenerateAsync(llmRequest, linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Iteration {Iteration} timed out after {Timeout}s for task {TaskId}",
                        iteration + 1, TimeoutSeconds, task.Id);
                    
                    return StrategyExecutionResult.CreateFailure(
                        $"Execution timed out after {TimeoutSeconds} seconds",
                        new List<string> { $"Completed {iteration} iterations before timeout" },
                        totalTokens,
                        totalCost,
                        stopwatch.Elapsed,
                        iteration);
                }

                totalTokens += llmResponse.TokensUsed;
                totalCost += llmResponse.CostUSD;

                _logger.LogDebug(
                    "Iteration {Iteration} LLM response: {Tokens} tokens, ${Cost}",
                    iteration + 1, llmResponse.TokensUsed, llmResponse.CostUSD);

                // Add assistant response to history
                conversationHistory.Add(new LlmMessage
                {
                    Role = "assistant",
                    Content = llmResponse.Content
                });

                // Parse code changes from response
                var codeChanges = ParseCodeChanges(llmResponse.Content);

                if (codeChanges.Count == 0)
                {
                    _logger.LogWarning(
                        "No code changes parsed from iteration {Iteration} for task {TaskId}",
                        iteration + 1, task.Id);

                    if (iteration == MaxIterations - 1)
                    {
                        return StrategyExecutionResult.CreateFailure(
                            "Failed to generate valid code changes",
                            new List<string> { "No parseable code changes found in LLM response" },
                            totalTokens,
                            totalCost,
                            stopwatch.Elapsed,
                            iteration + 1);
                    }

                    // Add feedback for next iteration
                    context.ValidationErrors.Add("No code changes found in response. Please provide code changes in the specified format.");
                    continue;
                }

                _logger.LogInformation(
                    "Parsed {ChangeCount} code changes from iteration {Iteration}",
                    codeChanges.Count, iteration + 1);

                // Validate the changes
                ValidationResult validationResult;
                try
                {
                    validationResult = await _validator.ValidateAsync(codeChanges, linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Validation timed out during iteration {Iteration} for task {TaskId}",
                        iteration + 1, task.Id);
                    
                    return StrategyExecutionResult.CreateFailure(
                        $"Validation timed out after {TimeoutSeconds} seconds",
                        new List<string> { $"Completed {iteration + 1} iterations before timeout" },
                        totalTokens,
                        totalCost,
                        stopwatch.Elapsed,
                        iteration + 1);
                }

                if (validationResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "Task {TaskId} completed successfully after {Iterations} iteration(s) in {Duration}ms. Tokens: {Tokens}, Cost: ${Cost}",
                        task.Id, iteration + 1, stopwatch.ElapsedMilliseconds, totalTokens, totalCost);

                    return StrategyExecutionResult.CreateSuccess(
                        codeChanges,
                        totalTokens,
                        totalCost,
                        stopwatch.Elapsed,
                        iteration + 1);
                }

                // Validation failed - add errors to context for next iteration
                _logger.LogWarning(
                    "Validation failed for iteration {Iteration} with {ErrorCount} error(s)",
                    iteration + 1, validationResult.Errors.Count);

                context.ValidationErrors.Clear();
                context.ValidationErrors.AddRange(validationResult.Errors);

                // If this was the last iteration, return failure
                if (iteration == MaxIterations - 1)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "Task {TaskId} failed after {MaxIterations} iterations. Final validation errors: {Errors}",
                        task.Id, MaxIterations, string.Join("; ", validationResult.Errors));

                    return StrategyExecutionResult.CreateFailure(
                        $"Max iterations ({MaxIterations}) exceeded without successful validation",
                        validationResult.Errors,
                        totalTokens,
                        totalCost,
                        stopwatch.Elapsed,
                        MaxIterations);
                }
            }

            // Should not reach here, but handle defensively
            stopwatch.Stop();
            return StrategyExecutionResult.CreateFailure(
                "Execution completed without returning a result",
                new List<string> { "Internal error: loop completed unexpectedly" },
                totalTokens,
                totalCost,
                stopwatch.Elapsed,
                MaxIterations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Task {TaskId} was cancelled by user", task.Id);
            
            return StrategyExecutionResult.CreateFailure(
                "Execution was cancelled",
                new List<string> { "User requested cancellation" },
                totalTokens,
                totalCost,
                stopwatch.Elapsed,
                context.Iteration);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during iterative execution for task {TaskId}", task.Id);
            
            return StrategyExecutionResult.CreateFailure(
                $"Unexpected error: {ex.Message}",
                new List<string> { ex.ToString() },
                totalTokens,
                totalCost,
                stopwatch.Elapsed,
                context.Iteration);
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert coding assistant. Generate precise code changes to solve the given task.

Output format:
For each file change, use this structure:
FILE: path/to/file.ext
```language
// Full file content or diff
```

Important:
- Be concise and only change what's necessary
- Ensure syntax is correct
- Follow best practices and coding standards
- Test your logic mentally before responding
- If you need to iterate, learn from validation errors in previous attempts";
    }

    private string BuildUserPrompt(CodingTask task, TaskExecutionContext context, int iteration)
    {
        var sb = new StringBuilder();

        if (iteration == 0)
        {
            // First iteration - provide full context
            sb.AppendLine($"Task: {task.Title}");
            sb.AppendLine($"Description: {task.Description}");
            sb.AppendLine($"Type: {task.Type}");
            sb.AppendLine($"Complexity: {task.Complexity}");
            sb.AppendLine();

            if (context.RelevantFiles.Count > 0)
            {
                sb.AppendLine("Relevant Files:");
                foreach (var file in context.RelevantFiles)
                {
                    sb.AppendLine($"## {file.Path}");
                    sb.AppendLine("```");
                    sb.AppendLine(file.Content);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }
        }
        else
        {
            // Subsequent iterations - focus on validation feedback
            sb.AppendLine($"The previous solution had validation errors. Please fix them:");
            sb.AppendLine();
            
            foreach (var error in context.ValidationErrors)
            {
                sb.AppendLine($"- {error}");
            }
            sb.AppendLine();
            sb.AppendLine("Please provide an updated solution that addresses these issues.");
        }

        return sb.ToString();
    }

    private List<CodeChange> ParseCodeChanges(string content)
    {
        var changes = new List<CodeChange>();
        
        try
        {
            var fileMatches = FilePatternRegex.Matches(content);
            var codeMatches = CodeBlockPatternRegex.Matches(content);

            // Log mismatch if FILE declarations and code blocks don't align
            if (fileMatches.Count != codeMatches.Count)
            {
                _logger.LogWarning(
                    "Mismatch between FILE declarations ({FileCount}) and code blocks ({CodeBlockCount}). " +
                    "Using minimum count to avoid silent data loss.",
                    fileMatches.Count, codeMatches.Count);
            }

            var matchCount = Math.Min(fileMatches.Count, codeMatches.Count);
            
            if (matchCount == 0)
            {
                _logger.LogWarning("No FILE declarations or code blocks found in LLM response");
                return changes;
            }

            for (int i = 0; i < matchCount; i++)
            {
                var filePath = fileMatches[i].Groups[1].Value.Trim();
                var language = codeMatches[i].Groups[1].Value;
                var code = codeMatches[i].Groups[2].Value;

                changes.Add(new CodeChange
                {
                    FilePath = filePath,
                    Language = string.IsNullOrWhiteSpace(language) ? null : language,
                    Content = code
                });
            }

            return changes;
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogError(ex, "Regex timeout while parsing code changes - possible DoS attempt or malformed input");
            return changes;
        }
    }
}
