using CodingAgent.Services.Memory.Domain.Entities;
using CodingAgent.Services.Memory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodingAgent.Services.Memory.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for procedural memory operations
/// </summary>
public class ProcedureRepository : IProcedureRepository
{
    private readonly MemoryDbContext _context;
    private readonly ILogger<ProcedureRepository> _logger;

    public ProcedureRepository(
        MemoryDbContext context,
        ILogger<ProcedureRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Procedure?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching procedure {ProcedureId}", id);
        return await _context.Procedures
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Procedure> AddAsync(Procedure procedure, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding procedure {ProcedureName}", procedure.ProcedureName);
        await _context.Procedures.AddAsync(procedure, ct);
        await _context.SaveChangesAsync(ct);
        return procedure;
    }

    public async Task UpdateAsync(Procedure procedure, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating procedure {ProcedureId}", procedure.Id);
        _context.Procedures.Update(procedure);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Procedure?> GetByContextAsync(
        Dictionary<string, object> contextPattern,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Searching for procedure matching context pattern");
        
        // Serialize context pattern for JSONB matching
        var contextJson = JsonSerializer.Serialize(contextPattern);
        
        // Find procedure where context pattern matches
        // For now, use a simple name-based search; can be enhanced with JSONB queries
        var allProcedures = await _context.Procedures
            .Where(p => p.SuccessRate > 0.5f) // Only consider procedures with some success
            .OrderByDescending(p => p.SuccessRate)
            .ThenByDescending(p => p.UsageCount)
            .Take(10)
            .ToListAsync(ct);
        
        // TODO: Implement proper JSONB matching when needed
        // For now, return the most successful procedure
        return allProcedures.FirstOrDefault();
    }

    public async Task<IEnumerable<Procedure>> GetTopProceduresAsync(
        int limit = 10,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching top {Limit} procedures", limit);
        return await _context.Procedures
            .OrderByDescending(p => p.SuccessRate)
            .ThenByDescending(p => p.UsageCount)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Procedure>> SearchByNameAsync(
        string searchTerm,
        int limit = 10,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Searching procedures by name: {SearchTerm}", searchTerm);
        return await _context.Procedures
            .Where(p => p.ProcedureName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.SuccessRate)
            .Take(limit)
            .ToListAsync(ct);
    }
}

