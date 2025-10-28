using System.Diagnostics;
using CodingAgent.Services.Dashboard.Application.DTOs;
using CodingAgent.Services.Dashboard.Domain.Services;
using CodingAgent.Services.Dashboard.Infrastructure.Caching;
using CodingAgent.Services.Dashboard.Infrastructure.ExternalServices;

namespace CodingAgent.Services.Dashboard.Application.Services;

/// <summary>
/// Service for aggregating dashboard data from multiple sources
/// </summary>
public class DashboardAggregationService : IDashboardAggregationService
{
    private readonly ChatServiceClient _chatClient;
    private readonly OrchestrationServiceClient _orchestrationClient;
    private readonly IDashboardCacheService _cache;
    private readonly ILogger<DashboardAggregationService> _logger;
    private readonly ActivitySource _activitySource;

    private const string StatsCacheKey = "dashboard:stats";
    private const string TasksCacheKeyPrefix = "dashboard:tasks:";
    private const string ActivityCacheKey = "dashboard:activity";

    public DashboardAggregationService(
        ChatServiceClient chatClient,
        OrchestrationServiceClient orchestrationClient,
        IDashboardCacheService cache,
        ILogger<DashboardAggregationService> logger,
        ActivitySource activitySource)
    {
        _chatClient = chatClient;
        _orchestrationClient = orchestrationClient;
        _cache = cache;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("DashboardAggregationService.GetStats");

        // Try cache first
        var cached = await _cache.GetAsync<DashboardStatsDto>(StatsCacheKey, ct);
        if (cached != null)
        {
            _logger.LogInformation("Returning cached dashboard stats");
            activity?.SetTag("cache.hit", true);
            return cached;
        }

        activity?.SetTag("cache.hit", false);
        _logger.LogInformation("Aggregating dashboard stats from services");

        // Fetch from both services in parallel
        var chatStatsTask = _chatClient.GetStatsAsync(ct);
        var orchestrationStatsTask = _orchestrationClient.GetStatsAsync(ct);

        await Task.WhenAll(chatStatsTask, orchestrationStatsTask);

        var chatStats = await chatStatsTask;
        var orchestrationStats = await orchestrationStatsTask;

        var stats = new DashboardStatsDto
        {
            TotalConversations = chatStats?.TotalConversations ?? 0,
            TotalMessages = chatStats?.TotalMessages ?? 0,
            TotalTasks = orchestrationStats?.TotalTasks ?? 0,
            TasksPending = orchestrationStats?.TasksPending ?? 0,
            TasksRunning = orchestrationStats?.TasksRunning ?? 0,
            TasksCompleted = orchestrationStats?.TasksCompleted ?? 0,
            TasksFailed = orchestrationStats?.TasksFailed ?? 0,
            LastUpdated = DateTime.UtcNow
        };

        // Cache for 5 minutes
        await _cache.SetAsync(StatsCacheKey, stats, TimeSpan.FromMinutes(5), ct);

        return stats;
    }

    public async Task<List<EnrichedTaskDto>> GetTasksAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("DashboardAggregationService.GetTasks");
        activity?.SetTag("page", page);
        activity?.SetTag("pageSize", pageSize);

        var cacheKey = $"{TasksCacheKeyPrefix}{page}:{pageSize}";

        // Try cache first
        var cached = await _cache.GetAsync<List<EnrichedTaskDto>>(cacheKey, ct);
        if (cached != null)
        {
            _logger.LogInformation("Returning cached tasks (page {Page})", page);
            activity?.SetTag("cache.hit", true);
            return cached;
        }

        activity?.SetTag("cache.hit", false);
        _logger.LogInformation("Fetching tasks from Orchestration Service (page {Page})", page);

        // Fetch tasks from orchestration service
        var tasks = await _orchestrationClient.GetTasksAsync(page, pageSize, ct);

        // Enrich with execution data (for now, mapping directly)
        var enrichedTasks = tasks.Select(t => new EnrichedTaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Type = t.Type,
            Complexity = t.Complexity,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            StartedAt = t.StartedAt,
            CompletedAt = t.CompletedAt,
            ExecutionCount = 0, // Would need executions endpoint
            LastExecutionStrategy = null,
            LastExecutionSuccess = null
        }).ToList();

        // Cache for 5 minutes
        await _cache.SetAsync(cacheKey, enrichedTasks, TimeSpan.FromMinutes(5), ct);

        return enrichedTasks;
    }

    public async Task<List<ActivityEventDto>> GetActivityAsync(int limit = 50, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("DashboardAggregationService.GetActivity");
        activity?.SetTag("limit", limit);

        // Try cache first
        var cached = await _cache.GetAsync<List<ActivityEventDto>>(ActivityCacheKey, ct);
        if (cached != null)
        {
            _logger.LogInformation("Returning cached activity events");
            activity?.SetTag("cache.hit", true);
            return cached.Take(limit).ToList();
        }

        activity?.SetTag("cache.hit", false);
        _logger.LogInformation("Generating activity events (placeholder implementation)");

        // Placeholder: In a real implementation, this would fetch from an event store
        // or aggregate from multiple service endpoints
        var events = new List<ActivityEventDto>();

        // Cache for 5 minutes
        await _cache.SetAsync(ActivityCacheKey, events, TimeSpan.FromMinutes(5), ct);

        return events;
    }
}
