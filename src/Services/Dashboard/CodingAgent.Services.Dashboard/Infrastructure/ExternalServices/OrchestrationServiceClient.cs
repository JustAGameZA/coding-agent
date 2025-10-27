using System.Diagnostics;
using System.Text.Json;

namespace CodingAgent.Services.Dashboard.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for Orchestration Service
/// </summary>
public class OrchestrationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrchestrationServiceClient> _logger;
    private readonly ActivitySource _activitySource;

    public OrchestrationServiceClient(HttpClient httpClient, ILogger<OrchestrationServiceClient> logger, ActivitySource activitySource)
    {
        _httpClient = httpClient;
        _logger = logger;
        _activitySource = activitySource;
    }

    public virtual async Task<OrchestrationStatsDto?> GetStatsAsync(CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("OrchestrationServiceClient.GetStats");
        
        try
        {
            _logger.LogInformation("Fetching stats from Orchestration Service");
            
            // Get tasks with pagination to calculate stats
            // Note: Orchestration service doesn't have dedicated /stats endpoint yet
            // Calculate stats from paginated task list (first 1000 tasks)
            var response = await _httpClient.GetAsync("/tasks?pageSize=100", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Orchestration Service returned {StatusCode}, using empty stats", response.StatusCode);
                return new OrchestrationStatsDto
                {
                    TotalTasks = 0,
                    TasksPending = 0,
                    TasksRunning = 0,
                    TasksCompleted = 0,
                    TasksFailed = 0
                };
            }
            
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>(ct);
            
            if (tasks == null || tasks.Count == 0)
            {
                return new OrchestrationStatsDto
                {
                    TotalTasks = 0,
                    TasksPending = 0,
                    TasksRunning = 0,
                    TasksCompleted = 0,
                    TasksFailed = 0
                };
            }
            
            // Calculate stats from task list
            var stats = new OrchestrationStatsDto
            {
                TotalTasks = tasks.Count,
                TasksPending = tasks.Count(t => t.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)),
                TasksRunning = tasks.Count(t => t.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) || 
                                                t.Status.Equals("Classifying", StringComparison.OrdinalIgnoreCase)),
                TasksCompleted = tasks.Count(t => t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)),
                TasksFailed = tasks.Count(t => t.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            };
            
            _logger.LogInformation("Orchestration stats: {TotalTasks} total, {Completed} completed, {Failed} failed, {Running} running",
                stats.TotalTasks, stats.TasksCompleted, stats.TasksFailed, stats.TasksRunning);
            
            return stats;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching Orchestration Service stats");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Orchestration Service stats");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return null;
        }
    }

    public virtual async Task<List<TaskDto>> GetTasksAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("OrchestrationServiceClient.GetTasks");
        activity?.SetTag("page", page);
        activity?.SetTag("pageSize", pageSize);
        
        try
        {
            _logger.LogInformation("Fetching tasks from Orchestration Service (page {Page}, size {PageSize})", page, pageSize);
            
            // Call Orchestration service GET /tasks with pagination
            var response = await _httpClient.GetAsync($"/tasks?page={page}&pageSize={pageSize}", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Orchestration Service returned {StatusCode} for tasks request", response.StatusCode);
                return new List<TaskDto>();
            }
            
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>(ct);
            
            if (tasks == null)
            {
                _logger.LogWarning("Orchestration Service returned null task list");
                return new List<TaskDto>();
            }
            
            _logger.LogInformation("Fetched {TaskCount} tasks from Orchestration Service", tasks.Count);
            
            return tasks;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching Orchestration Service tasks");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new List<TaskDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Orchestration Service tasks");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new List<TaskDto>();
        }
    }
}

public record OrchestrationStatsDto
{
    public int TotalTasks { get; init; }
    public int TasksPending { get; init; }
    public int TasksRunning { get; init; }
    public int TasksCompleted { get; init; }
    public int TasksFailed { get; init; }
}

public record TaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Complexity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
