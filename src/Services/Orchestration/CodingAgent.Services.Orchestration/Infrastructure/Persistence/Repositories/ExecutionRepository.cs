using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Repositories;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Orchestration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TaskExecution entity
/// </summary>
public class ExecutionRepository : IExecutionRepository
{
    private readonly OrchestrationDbContext _context;

    public ExecutionRepository(OrchestrationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TaskExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TaskExecution>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskExecution> AddAsync(TaskExecution entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await _context.Executions.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(TaskExecution entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _context.Executions.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskExecution entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _context.Executions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TaskExecution>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskExecution?> GetWithResultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TaskExecution>> GetByStrategyAsync(
        ExecutionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .Where(e => e.Strategy == strategy)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskExecution>> GetByStatusAsync(
        ExecutionStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskExecution?> GetLatestByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalCostByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .Where(e => e.TaskId == taskId && e.Result != null)
            .SumAsync(e => e.Result!.CostUSD, cancellationToken);
    }

    public async Task<int> GetTotalTokensByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Executions
            .Where(e => e.TaskId == taskId && e.Result != null)
            .SumAsync(e => e.Result!.TokensUsed, cancellationToken);
    }
}
