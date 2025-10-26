using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for fix attempts.
/// </summary>
public class FixAttemptRepository : IFixAttemptRepository
{
    private readonly CICDMonitorDbContext _context;

    public FixAttemptRepository(CICDMonitorDbContext context)
    {
        _context = context;
    }

    public async Task<FixAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FixAttempts
            .Include(fa => fa.BuildFailure)
            .FirstOrDefaultAsync(fa => fa.Id == id, cancellationToken);
    }

    public async Task<FixAttempt?> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.FixAttempts
            .Include(fa => fa.BuildFailure)
            .FirstOrDefaultAsync(fa => fa.TaskId == taskId, cancellationToken);
    }

    public async Task<FixAttempt> CreateAsync(FixAttempt fixAttempt, CancellationToken cancellationToken = default)
    {
        _context.FixAttempts.Add(fixAttempt);
        await _context.SaveChangesAsync(cancellationToken);
        return fixAttempt;
    }

    public async Task UpdateAsync(FixAttempt fixAttempt, CancellationToken cancellationToken = default)
    {
        _context.FixAttempts.Update(fixAttempt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FixStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var attempts = await _context.FixAttempts.ToListAsync(cancellationToken);

        return new FixStatistics
        {
            TotalAttempts = attempts.Count,
            Succeeded = attempts.Count(fa => fa.Status == FixStatus.Succeeded),
            Failed = attempts.Count(fa => fa.Status == FixStatus.Failed),
            InProgress = attempts.Count(fa => fa.Status == FixStatus.InProgress)
        };
    }

    public async Task<Dictionary<string, FixStatistics>> GetStatisticsByErrorPatternAsync(CancellationToken cancellationToken = default)
    {
        var attempts = await _context.FixAttempts
            .Where(fa => fa.ErrorPattern != null)
            .ToListAsync(cancellationToken);

        return attempts
            .GroupBy(fa => fa.ErrorPattern!)
            .ToDictionary(
                g => g.Key,
                g => new FixStatistics
                {
                    TotalAttempts = g.Count(),
                    Succeeded = g.Count(fa => fa.Status == FixStatus.Succeeded),
                    Failed = g.Count(fa => fa.Status == FixStatus.Failed),
                    InProgress = g.Count(fa => fa.Status == FixStatus.InProgress)
                });
    }
}
