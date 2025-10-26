using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for build failures.
/// </summary>
public class BuildFailureRepository : IBuildFailureRepository
{
    private readonly CICDMonitorDbContext _context;

    public BuildFailureRepository(CICDMonitorDbContext context)
    {
        _context = context;
    }

    public async Task<BuildFailure?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BuildFailures
            .FirstOrDefaultAsync(bf => bf.Id == id, cancellationToken);
    }

    public async Task<BuildFailure> CreateAsync(BuildFailure buildFailure, CancellationToken cancellationToken = default)
    {
        _context.BuildFailures.Add(buildFailure);
        await _context.SaveChangesAsync(cancellationToken);
        return buildFailure;
    }

    public async Task<List<BuildFailure>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        return await _context.BuildFailures
            .OrderByDescending(bf => bf.FailedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
