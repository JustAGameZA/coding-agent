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
        var aggregated = await _context.FixAttempts
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Succeeded = g.Sum(fa => fa.Status == FixStatus.Succeeded ? 1 : 0),
                Failed = g.Sum(fa => fa.Status == FixStatus.Failed ? 1 : 0),
                InProgress = g.Sum(fa => fa.Status == FixStatus.InProgress ? 1 : 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (aggregated is null)
        {
            return new FixStatistics
            {
                TotalAttempts = 0,
                Succeeded = 0,
                Failed = 0,
                InProgress = 0
            };
        }

        return new FixStatistics
        {
            TotalAttempts = aggregated.Total,
            Succeeded = aggregated.Succeeded,
            Failed = aggregated.Failed,
            InProgress = aggregated.InProgress
        };
    }

    public async Task<Dictionary<string, FixStatistics>> GetStatisticsByErrorPatternAsync(CancellationToken cancellationToken = default)
    {
        var aggregated = await _context.FixAttempts
            .Where(fa => fa.ErrorPattern != null)
            .GroupBy(fa => fa.ErrorPattern!)
            .Select(g => new
            {
                Key = g.Key,
                Total = g.Count(),
                Succeeded = g.Sum(fa => fa.Status == FixStatus.Succeeded ? 1 : 0),
                Failed = g.Sum(fa => fa.Status == FixStatus.Failed ? 1 : 0),
                InProgress = g.Sum(fa => fa.Status == FixStatus.InProgress ? 1 : 0)
            })
            .ToListAsync(cancellationToken);

        return aggregated.ToDictionary(
            x => x.Key,
            x => new FixStatistics
            {
                TotalAttempts = x.Total,
                Succeeded = x.Succeeded,
                Failed = x.Failed,
                InProgress = x.InProgress
            });
    }
}
