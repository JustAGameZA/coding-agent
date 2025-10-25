using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Tester agent that generates test cases for code changes.
/// Uses GPT-4o for test generation.
/// </summary>
public class TesterAgent : ITesterAgent
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<TesterAgent> _logger;
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

    public TesterAgent(ILlmClient llmClient, ILogger<TesterAgent> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> GenerateTestsAsync(
        List<CodeChange> changes,
        CodingTask originalTask,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "TesterAgent: Generating tests for {ChangeCount} code changes for task {TaskId}",
                changes.Count, originalTask.Id);

            if (changes.Count == 0)
            {
                _logger.LogWarning("TesterAgent: No changes to generate tests for");
                return new AgentResult
                {
                    AgentName = "Tester",
                    Success = true, // Not a failure - just nothing to test
                    Duration = stopwatch.Elapsed,
                    Output = "No code changes to test"
                };
            }

            var prompt = BuildTestPrompt(changes, originalTask);

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
                "TesterAgent: LLM response received. Tokens: {Tokens}, Cost: ${Cost}",
                response.TokensUsed, response.Cost);

            var testChanges = ParseTestCode(response.Content);

            stopwatch.Stop();

            _logger.LogInformation(
                "TesterAgent: Generated {TestCount} test files in {Duration}ms",
                testChanges.Count, stopwatch.ElapsedMilliseconds);

            return new AgentResult
            {
                AgentName = "Tester",
                Success = true,
                Changes = testChanges,
                TokensUsed = response.TokensUsed,
                Cost = response.Cost,
                Duration = stopwatch.Elapsed,
                Output = $"Generated {testChanges.Count} test files"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "TesterAgent: Failed to generate tests for task {TaskId}", originalTask.Id);

            return new AgentResult
            {
                AgentName = "Tester",
                Success = false,
                Duration = stopwatch.Elapsed,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private string BuildTestPrompt(List<CodeChange> changes, CodingTask originalTask)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Test Generation Request");
        sb.AppendLine();
        sb.AppendLine($"**Original Task**: {originalTask.Title}");
        sb.AppendLine($"**Description**: {originalTask.Description}");
        sb.AppendLine();
        sb.AppendLine("## Code to Test");
        sb.AppendLine();

        foreach (var change in changes)
        {
            sb.AppendLine($"### {change.FilePath}");
            if (!string.IsNullOrWhiteSpace(change.Language))
            {
                sb.AppendLine($"```{change.Language}");
            }
            else
            {
                sb.AppendLine("```");
            }
            sb.AppendLine(change.Content);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("Please generate comprehensive test cases for the above code.");

        return sb.ToString();
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert test engineer. Generate comprehensive test cases for the provided code changes.

Generate tests that cover:
1. **Happy path**: Normal successful scenarios
2. **Edge cases**: Boundary conditions and edge cases
3. **Error cases**: Invalid inputs and error handling
4. **Integration**: How components work together

Output format:
For each test file, use this structure:
FILE: path/to/TestFile.cs
```csharp
// Complete test file with all test cases
```

Rules:
- Use the appropriate testing framework (xUnit for C#, pytest for Python, etc.)
- Include setup/teardown if needed
- Use descriptive test method names
- Add assertions that verify expected behavior
- Mock external dependencies appropriately
- Follow testing best practices for the language
- Generate unit tests unless integration tests are specifically needed
- Include [Trait(""Category"", ""Unit"")] attribute for C# unit tests";
    }

    private List<CodeChange> ParseTestCode(string content)
    {
        var changes = new List<CodeChange>();

        try
        {
            // Use precompiled regex patterns
            var fileMatches = FileRegex.Matches(content);
            var codeMatches = CodeBlockRegex.Matches(content);

            _logger.LogDebug(
                "TesterAgent: Parsing - found {FileMatches} FILE declarations and {CodeBlocks} code blocks",
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
                    _logger.LogWarning("TesterAgent: No code block found after FILE: {FilePath}", filePath);
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
                    Type = ChangeType.Create // Tests are typically new files
                });
            }

            if (fileMatches.Count != codeMatches.Count)
            {
                _logger.LogWarning(
                    "TesterAgent: Mismatch between FILE declarations ({FileCount}) and code blocks ({CodeBlockCount})",
                    fileMatches.Count, codeMatches.Count);
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogError(ex, "TesterAgent: Regex timeout while parsing test code");
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
            _ => null
        };
    }
}
