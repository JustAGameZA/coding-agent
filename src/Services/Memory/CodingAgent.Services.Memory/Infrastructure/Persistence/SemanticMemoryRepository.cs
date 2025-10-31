using CodingAgent.Services.Memory.Domain.Entities;
using CodingAgent.Services.Memory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Memory.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for semantic memory operations with vector search
/// </summary>
public class SemanticMemoryRepository : ISemanticMemoryRepository
{
    private readonly MemoryDbContext _context;
    private readonly ILogger<SemanticMemoryRepository> _logger;

    public SemanticMemoryRepository(
        MemoryDbContext context,
        ILogger<SemanticMemoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SemanticMemory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching semantic memory {MemoryId}", id);
        return await _context.SemanticMemories
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<SemanticMemory> AddAsync(SemanticMemory memory, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding semantic memory {MemoryId} of type {ContentType}", memory.Id, memory.ContentType);
        await _context.SemanticMemories.AddAsync(memory, ct);
        await _context.SaveChangesAsync(ct);
        return memory;
    }

    public async Task UpdateAsync(SemanticMemory memory, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating semantic memory {MemoryId}", memory.Id);
        _context.SemanticMemories.Update(memory);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<SemanticMemory>> SearchAsync(
        float[] queryEmbedding,
        float threshold,
        int limit,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Searching semantic memory with threshold {Threshold}, limit {Limit}", threshold, limit);
        
        // TODO: Implement pgvector similarity search when pgvector extension is available
        // For now, return top results by confidence score as fallback
        // Vector similarity: SELECT * FROM semantic_memories 
        // WHERE embedding <-> @queryEmbedding < @threshold 
        // ORDER BY embedding <-> @queryEmbedding LIMIT @limit
        
        return await _context.SemanticMemories
            .Where(m => m.ConfidenceScore >= threshold)
            .OrderByDescending(m => m.ConfidenceScore)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<SemanticMemory>> GetByContentTypeAsync(
        string contentType,
        int limit = 100,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching semantic memories of type {ContentType}", contentType);
        return await _context.SemanticMemories
            .Where(m => m.ContentType == contentType)
            .OrderByDescending(m => m.ConfidenceScore)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<SemanticMemory>> GetBySourceEpisodeAsync(
        Guid episodeId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching semantic memories from episode {EpisodeId}", episodeId);
        return await _context.SemanticMemories
            .Where(m => m.SourceEpisodeId == episodeId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }
}

