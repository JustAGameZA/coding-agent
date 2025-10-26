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
}
