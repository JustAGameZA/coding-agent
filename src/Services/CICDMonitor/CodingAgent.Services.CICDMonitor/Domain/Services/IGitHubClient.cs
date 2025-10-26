namespace CodingAgent.Services.CICDMonitor.Domain.Services;

/// <summary>
/// Client for interacting with the GitHub service.
/// </summary>
public interface IGitHubClient
{
    /// <summary>
    /// Creates a pull request in the GitHub service.
    /// </summary>
    Task<CreatePullRequestResponse> CreatePullRequestAsync(CreatePullRequestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets repository metadata (includes DefaultBranch).
    /// </summary>
    Task<RepositoryInfo> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a pull request.
/// </summary>
public record CreatePullRequestRequest
{
    /// <summary>
    /// Repository owner.
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    /// Repository name.
    /// </summary>
    public required string Repo { get; init; }

    /// <summary>
    /// PR title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// PR body/description.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Head branch (source).
    /// </summary>
    public required string Head { get; init; }

    /// <summary>
    /// Base branch (target).
    /// </summary>
    public required string Base { get; init; }

    /// <summary>
    /// Whether to create as draft PR.
    /// </summary>
    public bool IsDraft { get; init; }
}

/// <summary>
/// Response from creating a pull request.
/// </summary>
public record CreatePullRequestResponse
{
    /// <summary>
    /// Pull request ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Pull request number.
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// Pull request HTML URL.
    /// </summary>
    public required string HtmlUrl { get; init; }

    /// <summary>
    /// Pull request title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Repository owner.
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    /// Repository name.
    /// </summary>
    public required string RepositoryName { get; init; }
}

/// <summary>
/// Minimal repository metadata required by CICD Monitor.
/// </summary>
public record RepositoryInfo
{
    public required string Owner { get; init; }
    public required string Name { get; init; }
    public required string DefaultBranch { get; init; }
}
