using CodingAgent.Services.GitHub.Domain.Entities;

namespace CodingAgent.Services.GitHub.Domain.Services;

/// <summary>
/// Interface for GitHub operations using Octokit
/// </summary>
public interface IGitHubService
{
    // Repository operations
    Task<Repository> CreateRepositoryAsync(string name, string? description = null, bool isPrivate = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<Repository>> ListRepositoriesAsync(CancellationToken cancellationToken = default);
    Task<Repository> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default);
    Task<Repository> UpdateRepositoryAsync(string owner, string name, string? description = null, CancellationToken cancellationToken = default);
    Task DeleteRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default);

    // Branch operations
    Task<Branch> CreateBranchAsync(string owner, string repo, string branchName, string sourceBranch, CancellationToken cancellationToken = default);
    Task<IEnumerable<Branch>> ListBranchesAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task DeleteBranchAsync(string owner, string repo, string branchName, CancellationToken cancellationToken = default);
    Task<Branch?> GetBranchAsync(string owner, string repo, string branchName, CancellationToken cancellationToken = default);

    // Pull Request operations
    Task<PullRequest> CreatePullRequestAsync(string owner, string repo, string title, string body, string head, string baseRef, bool isDraft = false, CancellationToken cancellationToken = default);
    Task<PullRequest> GetPullRequestAsync(string owner, string repo, int number, CancellationToken cancellationToken = default);
    Task<IEnumerable<PullRequest>> ListPullRequestsAsync(string owner, string repo, string? state = null, CancellationToken cancellationToken = default);
    Task<PullRequest> MergePullRequestAsync(string owner, string repo, int number, string mergeMethod = "merge", string? commitTitle = null, string? commitMessage = null, CancellationToken cancellationToken = default);
    Task ClosePullRequestAsync(string owner, string repo, int number, CancellationToken cancellationToken = default);
    Task AddCommentAsync(string owner, string repo, int number, string comment, CancellationToken cancellationToken = default);
    Task RequestReviewAsync(string owner, string repo, int number, IEnumerable<string> reviewers, CancellationToken cancellationToken = default);
    Task ApprovePullRequestAsync(string owner, string repo, int number, string? body = null, CancellationToken cancellationToken = default);
}
