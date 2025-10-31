using CodingAgent.Services.Memory.Domain.Entities;

namespace CodingAgent.Services.Memory.Domain.Repositories;

/// <summary>
/// Repository for episodic memory operations
/// </summary>
public interface IEpisodeRepository
{
    Task<Episode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Episode> AddAsync(Episode episode, CancellationToken ct = default);
    Task<IEnumerable<Episode>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<IEnumerable<Episode>> GetByExecutionIdAsync(Guid executionId, CancellationToken ct = default);
    Task<IEnumerable<Episode>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken ct = default);
    Task<IEnumerable<Episode>> GetSimilarAsync(string query, int limit = 10, CancellationToken ct = default);
}

