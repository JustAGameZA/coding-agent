using CodingAgent.Services.Memory.Domain.Entities;

namespace CodingAgent.Services.Memory.Domain.Repositories;

/// <summary>
/// Repository for semantic memory operations with vector search
/// </summary>
public interface ISemanticMemoryRepository
{
    Task<SemanticMemory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SemanticMemory> AddAsync(SemanticMemory memory, CancellationToken ct = default);
    Task UpdateAsync(SemanticMemory memory, CancellationToken ct = default);
    Task<IEnumerable<SemanticMemory>> SearchAsync(float[] queryEmbedding, float threshold, int limit, CancellationToken ct = default);
    Task<IEnumerable<SemanticMemory>> GetByContentTypeAsync(string contentType, int limit = 100, CancellationToken ct = default);
    Task<IEnumerable<SemanticMemory>> GetBySourceEpisodeAsync(Guid episodeId, CancellationToken ct = default);
}

