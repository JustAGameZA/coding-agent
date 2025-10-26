namespace CodingAgent.Services.CICDMonitor.Domain.Entities;

/// <summary>
/// Represents a build failure that occurred in CI/CD.
/// </summary>
public class BuildFailure
{
    /// <summary>
    /// Gets or sets the unique identifier for the build failure.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string Repository { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string Branch { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the commit SHA.
    /// </summary>
    public string CommitSha { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full error log.
    /// </summary>
    public string? ErrorLog { get; set; }

    /// <summary>
    /// Gets or sets the workflow name.
    /// </summary>
    public string? WorkflowName { get; set; }

    /// <summary>
    /// Gets or sets the job name.
    /// </summary>
    public string? JobName { get; set; }

    /// <summary>
    /// Gets or sets when the build failed.
    /// </summary>
    public DateTime FailedAt { get; set; }

    /// <summary>
    /// Gets or sets the error pattern matched (if any).
    /// </summary>
    public string? ErrorPattern { get; set; }
}
