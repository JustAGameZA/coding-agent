using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for build persistence operations.
/// </summary>
public class BuildRepository : IBuildRepository
{
    private readonly CICDMonitorDbContext _context;
    private readonly ILogger<BuildRepository> _logger;

    public BuildRepository(
        CICDMonitorDbContext context,
        ILogger<BuildRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Build?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Builds
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Build?> GetByWorkflowRunIdAsync(
        long workflowRunId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Builds
            .FirstOrDefaultAsync(b => b.WorkflowRunId == workflowRunId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Build>> GetRecentBuildsAsync(
        string owner,
        string repository,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Builds
            .Where(b => b.Owner == owner && b.Repository == repository)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Build>> GetAllRecentBuildsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Builds
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Build> AddAsync(Build build, CancellationToken cancellationToken = default)
    {
        _context.Builds.Add(build);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added build {BuildId} for {Owner}/{Repository} workflow run {WorkflowRunId}",
            build.Id,
            build.Owner,
            build.Repository,
            build.WorkflowRunId);

        return build;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Build build, CancellationToken cancellationToken = default)
    {
        _context.Builds.Update(build);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated build {BuildId} for {Owner}/{Repository}",
            build.Id,
            build.Owner,
            build.Repository);
    }

    /// <inheritdoc/>
    public async Task DeleteOldBuildsAsync(
        string owner,
        string repository,
        int retentionLimit = 100,
        CancellationToken cancellationToken = default)
    {
        var buildsToDelete = await _context.Builds
            .Where(b => b.Owner == owner && b.Repository == repository)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(retentionLimit)
            .ToListAsync(cancellationToken);

        if (buildsToDelete.Any())
        {
            _context.Builds.RemoveRange(buildsToDelete);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Deleted {Count} old builds for {Owner}/{Repository}",
                buildsToDelete.Count,
                owner,
                repository);
        }
    }
}
