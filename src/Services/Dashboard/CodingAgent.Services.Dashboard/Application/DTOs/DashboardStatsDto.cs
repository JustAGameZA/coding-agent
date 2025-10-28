namespace CodingAgent.Services.Dashboard.Application.DTOs;

/// <summary>
/// Aggregated statistics for the dashboard
/// </summary>
public record DashboardStatsDto
{
    public int TotalConversations { get; init; }
    public int TotalMessages { get; init; }
    public int TotalTasks { get; init; }
    public int TasksPending { get; init; }
    public int TasksRunning { get; init; }
    public int TasksCompleted { get; init; }
    public int TasksFailed { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Task with enriched execution data
/// </summary>
public record EnrichedTaskDto
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
    public int ExecutionCount { get; init; }
    public string? LastExecutionStrategy { get; init; }
    public bool? LastExecutionSuccess { get; init; }
}

/// <summary>
/// Recent activity event
/// </summary>
public record ActivityEventDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Source { get; init; } = string.Empty;
}
