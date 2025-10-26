namespace CodingAgent.Services.GitHub.Domain.Services;

/// <summary>
/// Interface for automated code review operations.
/// </summary>
public interface ICodeReviewService
{
    /// <summary>
    /// Analyzes a pull request and detects common issues.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="number">Pull request number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Code review result with detected issues.</returns>
    Task<CodeReviewResult> AnalyzePullRequestAsync(
        string owner,
        string repo,
        int number,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts review comments to a pull request.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="number">Pull request number.</param>
    /// <param name="result">Code review result with issues.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PostReviewCommentsAsync(
        string owner,
        string repo,
        int number,
        CodeReviewResult result,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of automated code review.
/// </summary>
public class CodeReviewResult
{
    /// <summary>
    /// Gets or sets whether the review should request changes.
    /// </summary>
    public bool RequestChanges { get; set; }

    /// <summary>
    /// Gets or sets the list of detected issues.
    /// </summary>
    public List<CodeReviewIssue> Issues { get; set; } = new();

    /// <summary>
    /// Gets or sets the summary comment.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code review issue.
/// </summary>
public class CodeReviewIssue
{
    /// <summary>
    /// Gets or sets the issue severity.
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// Gets or sets the issue type.
    /// </summary>
    public required string IssueType { get; set; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the line number (if applicable).
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the issue description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the suggested fix (if applicable).
    /// </summary>
    public string? Suggestion { get; set; }
}
