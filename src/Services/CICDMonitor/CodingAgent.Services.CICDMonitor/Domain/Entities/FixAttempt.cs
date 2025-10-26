namespace CodingAgent.Services.CICDMonitor.Domain.Entities;

/// <summary>
/// Represents an attempt to automatically fix a build failure.
/// </summary>
public class FixAttempt
{
    /// <summary>
    /// Gets or sets the unique identifier for the fix attempt.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the build failure ID.
    /// </summary>
    public Guid BuildFailureId { get; set; }

    /// <summary>
    /// Gets or sets the task ID created in the Orchestration service.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string Repository { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message being fixed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error pattern matched (if any).
    /// </summary>
    public string? ErrorPattern { get; set; }

    /// <summary>
    /// Gets or sets the fix status.
    /// </summary>
    public FixStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the fix was attempted.
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// Gets or sets when the fix was completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the pull request number (if successful).
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// Gets or sets the pull request URL (if successful).
    /// </summary>
    public string? PullRequestUrl { get; set; }

    /// <summary>
    /// Gets or sets the error message if the fix failed.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Navigation property to build failure.
    /// </summary>
    public BuildFailure? BuildFailure { get; set; }
}

/// <summary>
/// Status of a fix attempt.
/// </summary>
public enum FixStatus
{
    /// <summary>
    /// Fix is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Fix was successful and PR was created.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Fix failed to generate a solution.
    /// </summary>
    Failed
}
