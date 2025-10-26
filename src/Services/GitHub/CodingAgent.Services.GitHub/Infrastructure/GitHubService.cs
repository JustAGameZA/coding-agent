using CodingAgent.Services.GitHub.Domain.Entities;
using CodingAgent.Services.GitHub.Domain.Services;
using Octokit;

namespace CodingAgent.Services.GitHub.Infrastructure;

/// <summary>
/// Implementation of GitHub operations using Octokit.NET
/// </summary>
public class GitHubService : IGitHubService
{
    private readonly IGitHubClient _client;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(IGitHubClient client, ILogger<GitHubService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Repository Operations

    public async Task<Domain.Entities.Repository> CreateRepositoryAsync(
        string name,
        string? description = null,
        bool isPrivate = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating repository: {Name}", name);

            var newRepo = new NewRepository(name)
            {
                Description = description,
                Private = isPrivate,
                AutoInit = true
            };

            var repo = await _client.Repository.Create(newRepo);

            _logger.LogInformation("Successfully created repository: {FullName} (ID: {Id})", repo.FullName, repo.Id);

            return MapToRepository(repo);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create repository: {Name}", name);
            throw new InvalidOperationException($"Failed to create repository: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Domain.Entities.Repository>> ListRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing repositories for authenticated user");

            var repos = await _client.Repository.GetAllForCurrent();

            _logger.LogInformation("Found {Count} repositories", repos.Count);

            return repos.Select(MapToRepository);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list repositories");
            throw new InvalidOperationException($"Failed to list repositories: {ex.Message}", ex);
        }
    }

    public async Task<Domain.Entities.Repository> GetRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting repository: {Owner}/{Name}", owner, name);

            var repo = await _client.Repository.Get(owner, name);

            _logger.LogInformation("Successfully retrieved repository: {FullName}", repo.FullName);

            return MapToRepository(repo);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Repository not found: {Owner}/{Name}", owner, name);
            throw new InvalidOperationException($"Repository not found: {owner}/{name}", ex);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to get repository: {Owner}/{Name}", owner, name);
            throw new InvalidOperationException($"Failed to get repository: {ex.Message}", ex);
        }
    }

    public async Task<Domain.Entities.Repository> UpdateRepositoryAsync(
        string owner,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating repository: {Owner}/{Name}", owner, name);

            var update = new RepositoryUpdate
            {
                Name = name,
                Description = description
            };

            var repo = await _client.Repository.Edit(owner, name, update);

            _logger.LogInformation("Successfully updated repository: {FullName}", repo.FullName);

            return MapToRepository(repo);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update repository: {Owner}/{Name}", owner, name);
            throw new InvalidOperationException($"Failed to update repository: {ex.Message}", ex);
        }
    }

    public async Task DeleteRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting repository: {Owner}/{Name}", owner, name);

            await _client.Repository.Delete(owner, name);

            _logger.LogInformation("Successfully deleted repository: {Owner}/{Name}", owner, name);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete repository: {Owner}/{Name}", owner, name);
            throw new InvalidOperationException($"Failed to delete repository: {ex.Message}", ex);
        }
    }

    #endregion

    #region Branch Operations

    public async Task<Domain.Entities.Branch> CreateBranchAsync(
        string owner,
        string repo,
        string branchName,
        string sourceBranch,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating branch {BranchName} from {SourceBranch} in {Owner}/{Repo}",
                branchName, sourceBranch, owner, repo);

            // Get the source branch reference
            var sourceRef = await _client.Git.Reference.Get(owner, repo, $"heads/{sourceBranch}");

            // Create new branch reference
            var newRef = new NewReference($"refs/heads/{branchName}", sourceRef.Object.Sha);
            var createdRef = await _client.Git.Reference.Create(owner, repo, newRef);

            _logger.LogInformation("Successfully created branch: {BranchName}", branchName);

            return new Domain.Entities.Branch
            {
                Name = branchName,
                Sha = createdRef.Object.Sha,
                Protected = false
            };
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create branch: {BranchName} in {Owner}/{Repo}", branchName, owner, repo);
            throw new InvalidOperationException($"Failed to create branch: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Domain.Entities.Branch>> ListBranchesAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing branches for {Owner}/{Repo}", owner, repo);

            var branches = await _client.Repository.Branch.GetAll(owner, repo);

            _logger.LogInformation("Found {Count} branches", branches.Count);

            return branches.Select(b => new Domain.Entities.Branch
            {
                Name = b.Name,
                Sha = b.Commit.Sha,
                Protected = b.Protected
            });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list branches for {Owner}/{Repo}", owner, repo);
            throw new InvalidOperationException($"Failed to list branches: {ex.Message}", ex);
        }
    }

    public async Task DeleteBranchAsync(
        string owner,
        string repo,
        string branchName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);

            await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");

            _logger.LogInformation("Successfully deleted branch: {BranchName}", branchName);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete branch: {BranchName} from {Owner}/{Repo}", branchName, owner, repo);
            throw new InvalidOperationException($"Failed to delete branch: {ex.Message}", ex);
        }
    }

    public async Task<Domain.Entities.Branch?> GetBranchAsync(
        string owner,
        string repo,
        string branchName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);

            var branch = await _client.Repository.Branch.Get(owner, repo, branchName);

            _logger.LogInformation("Successfully retrieved branch: {BranchName}", branchName);

            return new Domain.Entities.Branch
            {
                Name = branch.Name,
                Sha = branch.Commit.Sha,
                Protected = branch.Protected
            };
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Branch not found: {BranchName} in {Owner}/{Repo}", branchName, owner, repo);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to get branch: {BranchName} from {Owner}/{Repo}", branchName, owner, repo);
            throw new InvalidOperationException($"Failed to get branch: {ex.Message}", ex);
        }
    }

    #endregion

    #region Helper Methods

    private static Domain.Entities.Repository MapToRepository(Octokit.Repository octokitRepo)
    {
        return new Domain.Entities.Repository
        {
            Id = Guid.NewGuid(), // Generate new GUID for our domain
            GitHubId = octokitRepo.Id,
            Owner = octokitRepo.Owner.Login,
            Name = octokitRepo.Name,
            FullName = octokitRepo.FullName,
            Description = octokitRepo.Description,
            CloneUrl = octokitRepo.CloneUrl,
            DefaultBranch = octokitRepo.DefaultBranch ?? "main",
            IsPrivate = octokitRepo.Private,
            CreatedAt = octokitRepo.CreatedAt.UtcDateTime,
            UpdatedAt = octokitRepo.UpdatedAt.UtcDateTime,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    #endregion
}
