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
/// SingleShot execution strategy for simple tasks (less than 50 LOC changes).
/// Makes a single LLM call to generate code changes with validation.
/// </summary>
public class SingleShotStrategy : IExecutionStrategy
{
    private readonly ILlmClient _llmClient;
    private readonly ICodeValidator _validator;
    private readonly ILogger<SingleShotStrategy> _logger;
    private readonly ActivitySource _activitySource;

    public string Name => "SingleShot";
    public TaskComplexity SupportsComplexity => TaskComplexity.Simple;

    public SingleShotStrategy(
        ILlmClient llmClient,
        ICodeValidator validator,
        ILogger<SingleShotStrategy> logger,
        ActivitySource activitySource)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
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

        using var activity = _activitySource.StartActivity("ExecuteSingleShot");
        activity?.SetTag("task.id", task.Id);
        activity?.SetTag("task.type", task.Type);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting SingleShot execution for task {TaskId} (Type: {TaskType}, Complexity: {Complexity})",
                task.Id, task.Type, task.Complexity);

            // 1. Build prompt from task and context
            var prompt = BuildPrompt(task, context);
            activity?.SetTag("prompt.length", prompt.Length);
            _logger.LogDebug("Built prompt with {PromptLength} characters", prompt.Length);

            // 2. Single LLM call
            _logger.LogInformation("Calling LLM with model gpt-4o-mini for task {TaskId}", task.Id);
            var llmRequest = new LlmRequest
            {
                Model = "gpt-4o-mini",
                Messages = new List<LlmMessage>
                {
                    new() { Role = "system", Content = GetSystemPrompt() },
                    new() { Role = "user", Content = prompt }
                },
                Temperature = 0.3,
                MaxTokens = 4000
            };

            var response = await _llmClient.GenerateAsync(llmRequest, cancellationToken);

            activity?.SetTag("tokens.used", response.TokensUsed);
            activity?.SetTag("cost.usd", response.Cost);
            _logger.LogInformation(
                "LLM call completed for task {TaskId}. Tokens: {Tokens}, Cost: ${Cost:F4}",
                task.Id, response.TokensUsed, response.Cost);

            // 3. Parse code changes from response
            var changes = ParseCodeChanges(response.Content);
            _logger.LogInformation("Parsed {ChangeCount} code changes from LLM response", changes.Count);

            if (changes.Count == 0)
            {
                var errorMessage = "No code changes could be parsed from LLM response";
                _logger.LogWarning(errorMessage);
                
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                return StrategyExecutionResult.CreateFailure(
                    errorMessage,
                    new List<string>(),
                    response.TokensUsed,
                    response.Cost,
                    stopwatch.Elapsed);
            }

            // 4. Validate changes
            _logger.LogDebug("Validating {ChangeCount} code changes", changes.Count);
            var validationResult = await _validator.ValidateAsync(changes, cancellationToken);

            if (!validationResult.IsSuccess)
            {
                var errorMessage = $"Code validation failed: {string.Join(", ", validationResult.Errors)}";
                _logger.LogWarning(
                    "Validation failed for task {TaskId}: {Errors}",
                    task.Id, string.Join("; ", validationResult.Errors));

                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                return StrategyExecutionResult.CreateFailure(
                    errorMessage,
                    validationResult.Errors.ToList(),
                    response.TokensUsed,
                    response.Cost,
                    stopwatch.Elapsed);
            }

            // 5. Return success result
            stopwatch.Stop();
            _logger.LogInformation(
                "SingleShot execution completed successfully for task {TaskId}. " +
                "Files: {Files}, Duration: {Duration}ms",
                task.Id, changes.Count, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return StrategyExecutionResult.CreateSuccess(
                changes,
                response.TokensUsed,
                response.Cost,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("SingleShot execution cancelled for task {TaskId}", task.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
            
            return StrategyExecutionResult.CreateFailure(
                "Execution was cancelled",
                new List<string>(),
                0,
                0,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "SingleShot execution failed for task {TaskId}", task.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return StrategyExecutionResult.CreateFailure(
                $"Execution failed: {ex.Message}",
                new List<string> { ex.ToString() },
                0,
                0,
                stopwatch.Elapsed);
        }
    }

    private string BuildPrompt(CodingTask task, TaskExecutionContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Task: {task.Title}");
        sb.AppendLine($"Description: {task.Description}");
        sb.AppendLine($"Type: {task.Type}");
        sb.AppendLine();

        if (context.RelevantFiles.Any())
        {
            sb.AppendLine("Relevant Files:");
            foreach (var file in context.RelevantFiles)
            {
                sb.AppendLine($"## {file.Path}");
                if (!string.IsNullOrWhiteSpace(file.Language))
                {
                    sb.AppendLine($"```{file.Language}");
                }
                else
                {
                    sb.AppendLine("```");
                }
                sb.AppendLine(file.Content);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        if (context.ValidationErrors.Any())
        {
            sb.AppendLine("Previous Validation Errors:");
            foreach (var error in context.ValidationErrors)
            {
                sb.AppendLine($"- {error}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert coding assistant. Generate precise code changes to solve the given task.

Output format:
For each file change, use this structure:
FILE: path/to/file.cs
```csharp
// Full file content or diff
```

Rules:
- Be concise and only change what's necessary
- Include proper error handling
- Follow best practices and coding standards
- Ensure code is properly formatted
- Add comments only where needed for clarity
- For simple tasks, prefer minimal changes

Generate the code changes now:";
    }

    private List<CodeChange> ParseCodeChanges(string content)
    {
        var changes = new List<CodeChange>();
        
        // Pattern to match FILE: declarations
                    var filePattern = @"FILE:\s*([^\r\n]+)";
        // Pattern to match code blocks with optional language
        var codePattern = @"```(\w+)?\r?\n(.*?)\r?\n```";

        var fileMatches = Regex.Matches(content, filePattern, RegexOptions.Multiline);
        var codeMatches = Regex.Matches(content, codePattern, RegexOptions.Singleline);

        _logger.LogDebug(
            "Parsing LLM response: found {FileMatches} file declarations and {CodeBlocks} code blocks",
            fileMatches.Count, codeMatches.Count);

        // Improved matching: Find code block immediately following each FILE declaration
        for (int fileIndex = 0; fileIndex < fileMatches.Count; fileIndex++)
        {
            var fileMatch = fileMatches[fileIndex];
            var filePath = fileMatch.Groups[1].Value.Trim();
            var filePosition = fileMatch.Index + fileMatch.Length;

            // Find the next code block after this FILE declaration
            Match? codeMatch = null;
            int closestDistance = int.MaxValue;

            foreach (Match potentialCodeMatch in codeMatches)
            {
                if (potentialCodeMatch.Index > filePosition)
                {
                    var distance = potentialCodeMatch.Index - filePosition;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        codeMatch = potentialCodeMatch;
                    }
                }
            }

            if (codeMatch == null)
            {
                _logger.LogWarning("No code block found after FILE declaration: {FilePath}", filePath);
                continue;
            }

            var language = codeMatch.Groups[1].Value;
            var codeContent = codeMatch.Groups[2].Value;

            if (string.IsNullOrWhiteSpace(language))
            {
                // Try to infer language from file extension
                language = InferLanguageFromPath(filePath);
            }

            changes.Add(new CodeChange
            {
                FilePath = filePath,
                Content = codeContent,
                Language = language,
                Type = ChangeType.Modify
            });
        }

        if (fileMatches.Count != codeMatches.Count)
        {
            _logger.LogWarning(
                "Mismatch between FILE declarations ({FileCount}) and code blocks ({CodeBlockCount})",
                fileMatches.Count, codeMatches.Count);
        }

        return changes;
    }

    private string? InferLanguageFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".java" => "java",
            ".cpp" or ".cc" or ".cxx" => "cpp",
            ".c" => "c",
            ".go" => "go",
            ".rs" => "rust",
            ".rb" => "ruby",
            ".php" => "php",
            ".swift" => "swift",
            ".kt" => "kotlin",
            ".sql" => "sql",
            ".json" => "json",
            ".xml" => "xml",
            ".html" => "html",
            ".css" => "css",
            _ => null
        };
    }
}
