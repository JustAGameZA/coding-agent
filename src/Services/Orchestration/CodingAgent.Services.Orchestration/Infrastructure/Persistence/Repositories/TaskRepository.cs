using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Repositories;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CodingTask entity
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly OrchestrationDbContext _context;

    public TaskRepository(OrchestrationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<CodingTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CodingTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .ToListAsync(cancellationToken);
    }

    public async Task<CodingTask> AddAsync(CodingTask entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await _context.Tasks.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(CodingTask entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _context.Tasks.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CodingTask entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _context.Tasks.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AnyAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CodingTask>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CodingTask>> GetByUserIdAndStatusAsync(
        Guid userId,
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId && t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CodingTask?> GetWithExecutionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Include(t => t.Executions)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CodingTask>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByStatusAsync(Guid userId, TaskStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .CountAsync(t => t.UserId == userId && t.Status == status, cancellationToken);
    }

    public async Task<PagedResult<CodingTask>> GetPagedByUserIdAsync(
        Guid userId,
        PaginationParameters pagination,
        TaskStatus? status = null,
        TaskType? type = null,
        CancellationToken cancellationToken = default)
    {
        if (pagination == null)
        {
            throw new ArgumentNullException(nameof(pagination));
        }

        // Build query with filters
        var query = _context.Tasks.Where(t => t.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated items
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<CodingTask>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }
}
