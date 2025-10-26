using CodingAgent.Services.CICDMonitor.Domain.ValueObjects;

namespace CodingAgent.Services.CICDMonitor.Domain.Entities;

/// <summary>
/// Represents a CI/CD build from GitHub Actions.
/// </summary>
public class Build
{
    /// <summary>
    /// Gets or sets the unique identifier for this build.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the GitHub workflow run ID.
    /// </summary>
    public long WorkflowRunId { get; set; }

    /// <summary>
    /// Gets or sets the repository owner.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

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
    /// Gets or sets the workflow name.
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the build status.
    /// </summary>
    public BuildStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the build conclusion (e.g., "success", "failure", "cancelled").
    /// </summary>
    public string? Conclusion { get; set; }

    /// <summary>
    /// Gets or sets the URL to the workflow run on GitHub.
    /// </summary>
    public string WorkflowUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parsed error messages from build logs.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets when the build was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the build was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the build started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the build completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
