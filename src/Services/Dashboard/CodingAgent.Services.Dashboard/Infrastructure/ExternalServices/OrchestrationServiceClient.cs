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
        var stats = new OrchestrationStatsDto
        {
            TotalTasks = 0,
            TasksPending = 0,
            TasksRunning = 0,
            TasksCompleted = 0,
            TasksFailed = 0
        };

        try
        {
            _logger.LogInformation("Fetching stats from Orchestration Service");

            // Aggregate over pages until fewer than pageSize results are returned (cap to avoid overload)
            const int pageSize = 100;
            const int maxPages = 10; // up to 1000 tasks
            var page = 1;
            var total = 0;
            var pending = 0;
            var running = 0;
            var completed = 0;
            var failed = 0;

            while (page <= maxPages)
            {
                var response = await _httpClient.GetAsync($"/tasks?page={page}&pageSize={pageSize}", ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Orchestration Service returned {StatusCode} for page {Page}", response.StatusCode, page);
                    break;
                }

                var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>(ct) ?? new List<TaskDto>();
                if (tasks.Count == 0)
                {
                    break;
                }

                total += tasks.Count;
                pending += tasks.Count(t => t.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
                running += tasks.Count(t => t.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) ||
                                           t.Status.Equals("Classifying", StringComparison.OrdinalIgnoreCase));
                completed += tasks.Count(t => t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
                failed += tasks.Count(t => t.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));

                if (tasks.Count < pageSize)
                {
                    break; // last page
                }

                page++;
            }

            stats = new OrchestrationStatsDto
            {
                TotalTasks = total,
                TasksPending = pending,
                TasksRunning = running,
                TasksCompleted = completed,
                TasksFailed = failed
            };

            _logger.LogInformation("Orchestration stats: {TotalTasks} total, {Completed} completed, {Failed} failed, {Running} running",
                stats.TotalTasks, stats.TasksCompleted, stats.TasksFailed, stats.TasksRunning);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching Orchestration Service stats");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Orchestration Service stats");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }

        return stats;
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
