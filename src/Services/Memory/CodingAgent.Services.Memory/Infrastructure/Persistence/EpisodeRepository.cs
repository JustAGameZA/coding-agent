using CodingAgent.Services.Memory.Domain.Entities;
using CodingAgent.Services.Memory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Memory.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for episodic memory operations
/// </summary>
public class EpisodeRepository : IEpisodeRepository
{
    private readonly MemoryDbContext _context;
    private readonly ILogger<EpisodeRepository> _logger;

    public EpisodeRepository(
        MemoryDbContext context,
        ILogger<EpisodeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Episode?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching episode {EpisodeId}", id);
        return await _context.Episodes
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Episode> AddAsync(Episode episode, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding episode {EpisodeId} for task {TaskId}", episode.Id, episode.TaskId);
        await _context.Episodes.AddAsync(episode, ct);
        await _context.SaveChangesAsync(ct);
        return episode;
    }

    public async Task<IEnumerable<Episode>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching episodes for task {TaskId}", taskId);
        return await _context.Episodes
            .Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Episode>> GetByExecutionIdAsync(Guid executionId, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching episodes for execution {ExecutionId}", executionId);
        return await _context.Episodes
            .Where(e => e.ExecutionId == executionId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Episode>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching recent episodes for user {UserId}", userId);
        return await _context.Episodes
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Episode>> GetSimilarAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        _logger.LogDebug("Searching for similar episodes with query: {Query}", query);
        
        // Basic text search - can be enhanced with full-text search or vector similarity
        // For now, search in learned patterns and context
        return await _context.Episodes
            .Where(e => 
                e.LearnedPatterns.Any(p => p.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                e.EventType.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}

