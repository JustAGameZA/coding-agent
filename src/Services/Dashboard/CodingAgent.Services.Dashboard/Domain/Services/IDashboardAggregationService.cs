using CodingAgent.Services.Dashboard.Application.DTOs;

namespace CodingAgent.Services.Dashboard.Domain.Services;

/// <summary>
/// Service for aggregating dashboard data from multiple sources
/// </summary>
public interface IDashboardAggregationService
{
    /// <summary>
    /// Get aggregated statistics for the dashboard
    /// </summary>
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get enriched tasks with execution data
    /// </summary>
    Task<List<EnrichedTaskDto>> GetTasksAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Get recent activity events from all services
    /// </summary>
    Task<List<ActivityEventDto>> GetActivityAsync(int limit = 50, CancellationToken ct = default);
}
