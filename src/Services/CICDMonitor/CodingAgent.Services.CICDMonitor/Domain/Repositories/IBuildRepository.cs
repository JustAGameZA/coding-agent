using CodingAgent.Services.CICDMonitor.Domain.Entities;

namespace CodingAgent.Services.CICDMonitor.Domain.Repositories;

/// <summary>
/// Repository interface for build persistence operations.
/// </summary>
public interface IBuildRepository
{
    /// <summary>
    /// Gets a build by its unique identifier.
    /// </summary>
    Task<Build?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a build by workflow run ID.
    /// </summary>
    Task<Build?> GetByWorkflowRunIdAsync(long workflowRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent builds for a repository, limited to the specified count.
    /// </summary>
    Task<IEnumerable<Build>> GetRecentBuildsAsync(
        string owner,
        string repository,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recent builds across all monitored repositories.
    /// </summary>
    Task<IEnumerable<Build>> GetAllRecentBuildsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new build to the repository.
    /// </summary>
    Task<Build> AddAsync(Build build, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing build.
    /// </summary>
    Task UpdateAsync(Build build, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old builds beyond the retention limit for a repository.
    /// </summary>
    Task DeleteOldBuildsAsync(
        string owner,
        string repository,
        int retentionLimit = 100,
        CancellationToken cancellationToken = default);
}
