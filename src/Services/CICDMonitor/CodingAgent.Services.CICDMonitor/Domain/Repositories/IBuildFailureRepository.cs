using CodingAgent.Services.CICDMonitor.Domain.Entities;

namespace CodingAgent.Services.CICDMonitor.Domain.Repositories;

/// <summary>
/// Repository for managing build failures.
/// </summary>
public interface IBuildFailureRepository
{
    /// <summary>
    /// Gets a build failure by ID.
    /// </summary>
    Task<BuildFailure?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new build failure record.
    /// </summary>
    Task<BuildFailure> CreateAsync(BuildFailure buildFailure, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent build failures.
    /// </summary>
    Task<List<BuildFailure>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
}
