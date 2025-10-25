using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

// Task DTOs

/// <summary>
/// Task data transfer object
/// </summary>
public record TaskDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskType Type { get; init; }
    public TaskComplexity Complexity { get; init; }
    public TaskStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int ExecutionCount { get; init; }
}

/// <summary>
/// Detailed task DTO with executions
/// </summary>
public record TaskDetailDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskType Type { get; init; }
    public TaskComplexity Complexity { get; init; }
    public TaskStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public List<ExecutionDto> Executions { get; init; } = new();
}

/// <summary>
/// Task execution data transfer object
/// </summary>
public record ExecutionDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public ExecutionStrategy Strategy { get; init; }
    public string ModelUsed { get; init; } = string.Empty;
    public ExecutionStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int TokensUsed { get; init; }
    public decimal CostUSD { get; init; }
}

// Request DTOs

/// <summary>
/// Request to create a new task
/// </summary>
/// <param name="Title">Task title (1-200 characters)</param>
/// <param name="Description">Task description (1-10,000 characters)</param>
public record CreateTaskRequest(string Title, string Description);

/// <summary>
/// Request to update an existing task
/// </summary>
/// <param name="Title">Task title (1-200 characters)</param>
/// <param name="Description">Task description (1-10,000 characters)</param>
public record UpdateTaskRequest(string Title, string Description);

/// <summary>
/// Request to execute a task
/// </summary>
/// <param name="Strategy">Optional: execution strategy to use. If not specified, will be auto-selected based on task complexity.</param>
public record ExecuteTaskRequest(ExecutionStrategy? Strategy);

/// <summary>
/// Response for task execution
/// </summary>
public record ExecuteTaskResponse
{
    public Guid TaskId { get; init; }
    public Guid ExecutionId { get; init; }
    public ExecutionStrategy Strategy { get; init; }
    public string Message { get; init; } = string.Empty;
}
