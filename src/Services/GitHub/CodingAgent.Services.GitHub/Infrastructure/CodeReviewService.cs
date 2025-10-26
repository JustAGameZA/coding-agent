using CodingAgent.Services.GitHub.Domain.Services;
using Octokit;
using System.Text;

namespace CodingAgent.Services.GitHub.Infrastructure;

/// <summary>
/// Implementation of automated code review service.
/// </summary>
public class CodeReviewService : ICodeReviewService
{
    private readonly IGitHubClient _client;
    private readonly ILogger<CodeReviewService> _logger;

    // Thresholds for code review
    private const int MaxFilesChanged = 50;
    private const int MaxLinesChanged = 1000;

    // File patterns to check
    private readonly string[] _testFilePatterns = { "test", "spec", ".test.", ".spec." };
    private readonly string[] _largeFileExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".zip", ".tar", ".gz" };

    public CodeReviewService(IGitHubClient client, ILogger<CodeReviewService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CodeReviewResult> AnalyzePullRequestAsync(
        string owner,
        string repo,
        int number,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing pull request #{Number} in {Owner}/{Repo}", number, owner, repo);

            var result = new CodeReviewResult();
            var issues = new List<CodeReviewIssue>();

            // Get PR details
            var pr = await _client.PullRequest.Get(owner, repo, number);
            
            // Get PR files
            var files = await _client.PullRequest.Files(owner, repo, number);

            // Check for too many files changed
            if (files.Count > MaxFilesChanged)
            {
                issues.Add(new CodeReviewIssue
                {
                    Severity = "warning",
                    IssueType = "large_pr",
                    FilePath = "",
                    Description = $"This PR changes {files.Count} files. Consider splitting into smaller PRs for easier review.",
                    Suggestion = "Break this PR into smaller, focused changes."
                });
            }

            // Check total lines changed
            var totalLinesChanged = pr.Additions + pr.Deletions;
            if (totalLinesChanged > MaxLinesChanged)
            {
                issues.Add(new CodeReviewIssue
                {
                    Severity = "warning",
                    IssueType = "large_pr",
                    FilePath = "",
                    Description = $"This PR changes {totalLinesChanged} lines. Large PRs are harder to review thoroughly.",
                    Suggestion = "Consider breaking this into smaller PRs."
                });
            }

            // Analyze each file
            var hasTests = false;
            foreach (var file in files)
            {
                // Check if this is a test file
                if (IsTestFile(file.FileName))
                {
                    hasTests = true;
                }

                // Check for large files
                if (file.Changes > 500)
                {
                    issues.Add(new CodeReviewIssue
                    {
                        Severity = "info",
                        IssueType = "large_file",
                        FilePath = file.FileName,
                        Description = $"File has {file.Changes} line changes. Large file changes are harder to review."
                    });
                }

                // Check for binary/large files
                if (IsLargeFile(file.FileName))
                {
                    issues.Add(new CodeReviewIssue
                    {
                        Severity = "warning",
                        IssueType = "binary_file",
                        FilePath = file.FileName,
                        Description = "Large binary file detected. Consider if this file is necessary in the repository.",
                        Suggestion = "Use Git LFS for large binary files or store them externally."
                    });
                }

                // Check for deleted files without explanation
                if (file.Status == "removed")
                {
                    _logger.LogDebug("File removed: {FileName}", file.FileName);
                }
            }

            // Check for missing tests (if code files were modified)
            var hasCodeChanges = files.Any(f => IsCodeFile(f.FileName) && f.Status != "removed");
            if (hasCodeChanges && !hasTests)
            {
                issues.Add(new CodeReviewIssue
                {
                    Severity = "warning",
                    IssueType = "missing_tests",
                    FilePath = "",
                    Description = "No test files were modified. Consider adding tests for your changes.",
                    Suggestion = "Add unit tests or integration tests for the new functionality."
                });
            }

            // Check PR description
            if (string.IsNullOrWhiteSpace(pr.Body))
            {
                issues.Add(new CodeReviewIssue
                {
                    Severity = "info",
                    IssueType = "missing_description",
                    FilePath = "",
                    Description = "PR description is empty. A good description helps reviewers understand the changes.",
                    Suggestion = "Add a description explaining what changed and why."
                });
            }

            result.Issues = issues;
            result.RequestChanges = issues.Any(i => i.Severity == "error");

            // Build summary
            result.Summary = BuildSummary(pr, files.Count, issues);

            _logger.LogInformation("Code review analysis completed. Found {IssueCount} issues", issues.Count);

            return result;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to analyze pull request #{Number} in {Owner}/{Repo}", number, owner, repo);
            throw new InvalidOperationException($"Failed to analyze pull request: {ex.Message}", ex);
        }
    }

    public async Task PostReviewCommentsAsync(
        string owner,
        string repo,
        int number,
        CodeReviewResult result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Posting review comments for PR #{Number} in {Owner}/{Repo}", number, owner, repo);

            if (result.Issues.Count == 0)
            {
                // Post approval comment
                await _client.Issue.Comment.Create(owner, repo, number,
                    "✅ **Automated Code Review**: No issues detected. Good job!");
                
                _logger.LogInformation("Posted approval comment");
                return;
            }

            // Post review with comments
            var review = new PullRequestReviewCreate
            {
                Body = result.Summary,
                Event = result.RequestChanges ? PullRequestReviewEvent.RequestChanges : PullRequestReviewEvent.Comment
            };

            await _client.PullRequest.Review.Create(owner, repo, number, review);

            // Post individual comments for file-specific issues
            foreach (var issue in result.Issues.Where(i => !string.IsNullOrEmpty(i.FilePath)))
            {
                var commentBody = $"**{issue.Severity.ToUpper()}**: {issue.IssueType}\n\n{issue.Description}";
                if (!string.IsNullOrEmpty(issue.Suggestion))
                {
                    commentBody += $"\n\n💡 **Suggestion**: {issue.Suggestion}";
                }

                await _client.Issue.Comment.Create(owner, repo, number, commentBody);
            }

            _logger.LogInformation("Posted {CommentCount} review comments", result.Issues.Count);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to post review comments for PR #{Number} in {Owner}/{Repo}", number, owner, repo);
            throw new InvalidOperationException($"Failed to post review comments: {ex.Message}", ex);
        }
    }

    private string BuildSummary(Octokit.PullRequest pr, int filesChanged, List<CodeReviewIssue> issues)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🤖 **Automated Code Review**");
        sb.AppendLine();
        sb.AppendLine("### Summary");
        sb.AppendLine($"- Files changed: {filesChanged}");
        sb.AppendLine($"- Lines added: +{pr.Additions}");
        sb.AppendLine($"- Lines removed: -{pr.Deletions}");
        sb.AppendLine();

        if (issues.Count == 0)
        {
            sb.AppendLine("✅ No issues detected!");
        }
        else
        {
            var errors = issues.Count(i => i.Severity == "error");
            var warnings = issues.Count(i => i.Severity == "warning");
            var infos = issues.Count(i => i.Severity == "info");

            sb.AppendLine("### Issues Detected");
            if (errors > 0)
            {
                sb.AppendLine($"- ❌ Errors: {errors}");
            }
            if (warnings > 0)
            {
                sb.AppendLine($"- ⚠️ Warnings: {warnings}");
            }
            if (infos > 0)
            {
                sb.AppendLine($"- ℹ️ Info: {infos}");
            }
            sb.AppendLine();

            sb.AppendLine("### Details");
            foreach (var issue in issues)
            {
                var icon = issue.Severity switch
                {
                    "error" => "❌",
                    "warning" => "⚠️",
                    _ => "ℹ️"
                };

                sb.AppendLine($"{icon} **{issue.IssueType}**");
                if (!string.IsNullOrEmpty(issue.FilePath))
                {
                    sb.AppendLine($"  - File: `{issue.FilePath}`");
                }
                sb.AppendLine($"  - {issue.Description}");
                if (!string.IsNullOrEmpty(issue.Suggestion))
                {
                    sb.AppendLine($"  - 💡 Suggestion: {issue.Suggestion}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private bool IsTestFile(string fileName)
    {
        var lowerFileName = fileName.ToLowerInvariant();
        return _testFilePatterns.Any(pattern => lowerFileName.Contains(pattern));
    }

    private bool IsCodeFile(string fileName)
    {
        var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".go", ".rb", ".php", ".cpp", ".c", ".h" };
        return codeExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsLargeFile(string fileName)
    {
        return _largeFileExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
