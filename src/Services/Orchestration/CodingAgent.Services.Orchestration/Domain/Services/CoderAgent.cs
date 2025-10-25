using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CodingAgent.Services.Orchestration.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Coder agent that implements code changes for subtasks.
/// Uses GPT-4o for code generation.
/// </summary>
public class CoderAgent : ICoderAgent
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<CoderAgent> _logger;
    private const string ModelName = "gpt-4o";
    private const double Temperature = 0.3;
    private const int MaxTokens = 4000;

    // Precompiled regex patterns for parsing
    private static readonly Regex FileRegex = new Regex(
        @"FILE:\s*([^\r\n]+)",
        RegexOptions.Multiline | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex CodeBlockRegex = new Regex(
        @"```(\w+)?\r?\n(.*?)\r?\n```",
        RegexOptions.Singleline | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    public CoderAgent(ILlmClient llmClient, ILogger<CoderAgent> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> ImplementSubTaskAsync(
        SubTask subTask,
        TaskExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "CoderAgent: Implementing subtask {SubTaskId} - {Title}",
                subTask.Id, subTask.Title);

            var prompt = BuildCodingPrompt(subTask, context);

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
                "CoderAgent: LLM response received for subtask {SubTaskId}. Tokens: {Tokens}, Cost: ${Cost}",
                subTask.Id, response.TokensUsed, response.Cost);

            var changes = ParseCodeChanges(response.Content);

            stopwatch.Stop();

            _logger.LogInformation(
                "CoderAgent: Implemented subtask {SubTaskId} with {ChangeCount} changes in {Duration}ms",
                subTask.Id, changes.Count, stopwatch.ElapsedMilliseconds);

            return new AgentResult
            {
                AgentName = $"Coder-{subTask.Id}",
                Success = true,
                Changes = changes,
                TokensUsed = response.TokensUsed,
                Cost = response.Cost,
                Duration = stopwatch.Elapsed,
                Output = $"Implemented {changes.Count} file changes"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "CoderAgent: Failed to implement subtask {SubTaskId}", subTask.Id);

            return new AgentResult
            {
                AgentName = $"Coder-{subTask.Id}",
                Success = false,
                Duration = stopwatch.Elapsed,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private string BuildCodingPrompt(SubTask subTask, TaskExecutionContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Code Implementation Request");
        sb.AppendLine();
        sb.AppendLine($"**Subtask**: {subTask.Title}");
        sb.AppendLine($"**Description**: {subTask.Description}");
        sb.AppendLine();

        if (subTask.AffectedFiles.Any())
        {
            sb.AppendLine("## Files to Modify");
            foreach (var file in subTask.AffectedFiles)
            {
                sb.AppendLine($"- {file}");
            }
            sb.AppendLine();
        }

        if (context.RelevantFiles.Any())
        {
            sb.AppendLine("## Existing Code");
            foreach (var file in context.RelevantFiles.Where(f => 
                subTask.AffectedFiles.Contains(f.Path) || subTask.AffectedFiles.Count == 0))
            {
                sb.AppendLine($"### {file.Path}");
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

        sb.AppendLine("Please implement the code changes for this subtask.");

        return sb.ToString();
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert software engineer. Generate precise code changes to implement the given subtask.

Output format:
For each file change, use this structure:
FILE: path/to/file.ext
```language
// Full file content or specific changes
```

Rules:
- Be concise and only change what's necessary for this specific subtask
- Include proper error handling
- Follow best practices and coding standards
- Ensure code is properly formatted
- Add comments only where needed for clarity
- Focus on the subtask scope - don't implement unrelated features
- If creating new files, provide complete implementations
- If modifying existing files, show the full updated file content";
    }

    private List<CodeChange> ParseCodeChanges(string content)
    {
        var changes = new List<CodeChange>();

        try
        {
            // Use precompiled regex patterns
            var fileMatches = FileRegex.Matches(content);
            var codeMatches = CodeBlockRegex.Matches(content);

            _logger.LogDebug(
                "CoderAgent: Parsing - found {FileMatches} FILE declarations and {CodeBlocks} code blocks",
                fileMatches.Count, codeMatches.Count);

            // Match each FILE declaration with its following code block
            for (int fileIndex = 0; fileIndex < fileMatches.Count; fileIndex++)
            {
                var fileMatch = fileMatches[fileIndex];
                var filePath = fileMatch.Groups[1].Value.Trim();
                var filePosition = fileMatch.Index + fileMatch.Length;

                // Find the nearest code block after this FILE declaration
                Match? codeMatch = null;
                int closestDistance = int.MaxValue;

                for (int i = 0; i < codeMatches.Count; i++)
                {
                    var potentialCodeMatch = codeMatches[i];
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
                    _logger.LogWarning("CoderAgent: No code block found after FILE: {FilePath}", filePath);
                    continue;
                }

                var language = codeMatch.Groups[1].Value;
                var codeContent = codeMatch.Groups[2].Value;

                if (string.IsNullOrWhiteSpace(language))
                {
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
                    "CoderAgent: Mismatch between FILE declarations ({FileCount}) and code blocks ({CodeBlockCount})",
                    fileMatches.Count, codeMatches.Count);
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogError(ex, "CoderAgent: Regex timeout while parsing code changes");
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
