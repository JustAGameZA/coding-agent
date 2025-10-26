namespace CodingAgent.Services.GitHub.Domain.Entities;

/// <summary>
/// Represents a GitHub Pull Request domain entity.
/// </summary>
public class PullRequest
{
    /// <summary>
    /// Gets or sets the internal ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the GitHub PR ID.
    /// </summary>
    public long GitHubId { get; set; }

    /// <summary>
    /// Gets or sets the PR number.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the repository owner.
    /// </summary>
    public required string Owner { get; set; }

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public required string RepositoryName { get; set; }

    /// <summary>
    /// Gets or sets the PR title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the PR description/body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the head branch (source).
    /// </summary>
    public required string Head { get; set; }

    /// <summary>
    /// Gets or sets the base branch (target).
    /// </summary>
    public required string Base { get; set; }

    /// <summary>
    /// Gets or sets the PR state (open, closed).
    /// </summary>
    public required string State { get; set; }

    /// <summary>
    /// Gets or sets whether the PR is merged.
    /// </summary>
    public bool IsMerged { get; set; }

    /// <summary>
    /// Gets or sets whether the PR is draft.
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// Gets or sets the PR author.
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// Gets or sets the PR URL.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets the HTML URL.
    /// </summary>
    public required string HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets when the PR was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the PR was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the PR was merged (if applicable).
    /// </summary>
    public DateTime? MergedAt { get; set; }

    /// <summary>
    /// Gets or sets when the PR was closed (if applicable).
    /// </summary>
    public DateTime? ClosedAt { get; set; }
}
