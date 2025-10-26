using CodingAgent.SharedKernel.Domain.ValueObjects;

namespace CodingAgent.Services.CICDMonitor.Domain.Services;

/// <summary>
/// Client for interacting with the Orchestration service.
/// </summary>
public interface IOrchestrationClient
{
    /// <summary>
    /// Creates a new task in the Orchestration service.
    /// </summary>
    Task<CreateTaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a task in the Orchestration service.
/// </summary>
public record CreateTaskRequest
{
    /// <summary>
    /// Task title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Task description.
    /// </summary>
    public required string Description { get; init; }
}

/// <summary>
/// Response from creating a task in the Orchestration service.
/// </summary>
public record CreateTaskResponse
{
    /// <summary>
    /// The created task ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Task title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Task description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Task type.
    /// </summary>
    public required TaskType Type { get; init; }

    /// <summary>
    /// Task complexity.
    /// </summary>
    public required TaskComplexity Complexity { get; init; }

    /// <summary>
    /// Task status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// When the task was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the task was last updated.
    /// </summary>
    public required DateTime UpdatedAt { get; init; }
}
