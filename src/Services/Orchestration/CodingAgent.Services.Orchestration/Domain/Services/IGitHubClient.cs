namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Interface for interacting with GitHub service.
/// </summary>
public interface IGitHubClient
{
    /// <summary>
    /// Creates a pull request on GitHub.
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="title">PR title</param>
    /// <param name="body">PR description</param>
    /// <param name="head">Head branch (source)</param>
    /// <param name="base">Base branch (target)</param>
    /// <param name="isDraft">Whether to create as draft</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PR number and URL</returns>
    Task<GitHubPullRequest> CreatePullRequestAsync(
        string owner,
        string repo,
        string title,
        string body,
        string head,
        string @base,
        bool isDraft = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the GitHub service is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a created GitHub pull request.
/// </summary>
public record GitHubPullRequest(
    int Number,
    string Url,
    string HtmlUrl);
