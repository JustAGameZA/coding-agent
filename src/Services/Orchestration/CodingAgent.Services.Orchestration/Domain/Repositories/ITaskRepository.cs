using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Results;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Domain.Repositories;

/// <summary>
/// Repository interface for CodingTask entity
/// </summary>
public interface ITaskRepository : IRepository<CodingTask>
{
    /// <summary>
    /// Gets all tasks for a specific user
    /// </summary>
    Task<IEnumerable<CodingTask>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks by status for a specific user
    /// </summary>
    Task<IEnumerable<CodingTask>> GetByUserIdAndStatusAsync(
        Guid userId,
        TaskStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task with all its executions
    /// </summary>
    Task<CodingTask?> GetWithExecutionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks created within a date range
    /// </summary>
    Task<IEnumerable<CodingTask>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts tasks by status for a user
    /// </summary>
    Task<int> CountByStatusAsync(Guid userId, TaskStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated tasks for a specific user with optional filtering
    /// </summary>
    Task<PagedResult<CodingTask>> GetPagedByUserIdAsync(
        Guid userId,
        PaginationParameters pagination,
        TaskStatus? status = null,
        TaskType? type = null,
        CancellationToken cancellationToken = default);
}
