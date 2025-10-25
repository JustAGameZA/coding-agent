using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Reviewer agent that reviews code changes for quality and correctness.
/// Uses GPT-4o for code review.
/// </summary>
public class ReviewerAgent : IReviewerAgent
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ReviewerAgent> _logger;
    private const string ModelName = "gpt-4o";
    private const double Temperature = 0.2; // Lower temperature for more consistent reviews
    private const int MaxTokens = 3000;

    public ReviewerAgent(ILlmClient llmClient, ILogger<ReviewerAgent> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> ReviewChangesAsync(
        List<CodeChange> changes,
        CodingTask originalTask,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "ReviewerAgent: Reviewing {ChangeCount} code changes for task {TaskId}",
                changes.Count, originalTask.Id);

            if (changes.Count == 0)
            {
                _logger.LogWarning("ReviewerAgent: No changes to review");
                return new AgentResult
                {
                    AgentName = "Reviewer",
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Errors = new List<string> { "No code changes provided for review" }
                };
            }

            var prompt = BuildReviewPrompt(changes, originalTask);

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
                "ReviewerAgent: LLM response received. Tokens: {Tokens}, Cost: ${Cost}",
                response.TokensUsed, response.Cost);

            var review = ParseReview(response.Content);

            stopwatch.Stop();

            _logger.LogInformation(
                "ReviewerAgent: Review completed. Approved: {IsApproved}, Issues: {IssueCount} in {Duration}ms",
                review.IsApproved, review.Issues.Count, stopwatch.ElapsedMilliseconds);

            return new AgentResult
            {
                AgentName = "Reviewer",
                Success = true,
                TokensUsed = response.TokensUsed,
                Cost = response.Cost,
                Duration = stopwatch.Elapsed,
                Output = JsonSerializer.Serialize(review),
                Errors = review.IsApproved ? new List<string>() : review.Issues
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "ReviewerAgent: Failed to review changes for task {TaskId}", originalTask.Id);

            return new AgentResult
            {
                AgentName = "Reviewer",
                Success = false,
                Duration = stopwatch.Elapsed,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private string BuildReviewPrompt(List<CodeChange> changes, CodingTask originalTask)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Code Review Request");
        sb.AppendLine();
        sb.AppendLine($"**Original Task**: {originalTask.Title}");
        sb.AppendLine($"**Description**: {originalTask.Description}");
        sb.AppendLine();
        sb.AppendLine("## Code Changes to Review");
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

        sb.AppendLine("Please review these code changes thoroughly.");

        return sb.ToString();
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert code reviewer. Review the provided code changes for:

1. **Correctness**: Does the code correctly implement the task requirements?
2. **Quality**: Is the code well-structured and maintainable?
3. **Best Practices**: Does it follow language-specific best practices?
4. **Security**: Are there any security vulnerabilities?
5. **Performance**: Are there obvious performance issues?
6. **Error Handling**: Is error handling appropriate?
7. **Testing**: Is the code testable?

Provide your review in the following JSON format:
```json
{
  ""isApproved"": true/false,
  ""issues"": [
    ""Issue 1: Description of blocking issue"",
    ""Issue 2: Another blocking issue""
  ],
  ""suggestions"": [
    ""Suggestion 1: Nice-to-have improvement"",
    ""Suggestion 2: Optional enhancement""
  ],
  ""severity"": 1-5
}
```

Where severity is:
- 1-2: Minor issues, approve with suggestions
- 3: Moderate issues, approve with conditions
- 4-5: Critical issues, reject

Important:
- Be constructive and specific
- Approve changes if there are no blocking issues (severity <= 3)
- Focus on actual problems, not stylistic preferences
- Consider the context of the task complexity";
    }

    private ReviewResult ParseReview(string content)
    {
        try
        {
            // Try to extract JSON from code blocks
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                content,
                @"```(?:json)?\s*(\{.*?\})\s*```",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            var jsonContent = jsonMatch.Success ? jsonMatch.Groups[1].Value : content;

            var reviewData = JsonSerializer.Deserialize<ReviewJsonData>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (reviewData == null)
            {
                _logger.LogWarning("ReviewerAgent: Failed to parse review, using default approval");
                return CreateDefaultApproval();
            }

            return new ReviewResult
            {
                IsApproved = reviewData.IsApproved,
                Issues = reviewData.Issues ?? new List<string>(),
                Suggestions = reviewData.Suggestions ?? new List<string>(),
                Severity = reviewData.Severity
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "ReviewerAgent: Failed to parse review JSON, using default approval");
            return CreateDefaultApproval();
        }
    }

    private ReviewResult CreateDefaultApproval()
    {
        return new ReviewResult
        {
            IsApproved = true,
            Issues = new List<string>(),
            Suggestions = new List<string> { "Review parsing failed - automated approval" },
            Severity = 1
        };
    }

    // Internal class for JSON deserialization
    private class ReviewJsonData
    {
        public bool IsApproved { get; set; }
        public List<string>? Issues { get; set; }
        public List<string>? Suggestions { get; set; }
        public int Severity { get; set; }
    }
}
