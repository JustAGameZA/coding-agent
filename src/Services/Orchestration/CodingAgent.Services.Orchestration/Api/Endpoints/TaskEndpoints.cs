using Microsoft.AspNetCore.Mvc;
using CodingAgent.Services.Orchestration.Domain.Repositories;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using CodingAgent.SharedKernel.Results;
using CodingAgent.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using FluentValidation;
using TaskStatus = CodingAgent.Services.Orchestration.Domain.Entities.TaskStatus;
using CodingAgent.Services.Orchestration.Infrastructure.Logging;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

/// <summary>
/// Task management endpoints for the Orchestration service.
/// </summary>
public static class TaskEndpoints
{
    // TODO: Replace with authenticated user from JWT when auth is implemented
    private const string DefaultUserId = "00000000-0000-0000-0000-000000000001";

    /// <summary>
    /// Maps task-related endpoints.
    /// </summary>
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks")
            .WithTags("Tasks")
            .WithOpenApi()
            .WithDescription("Endpoints for managing coding tasks and their execution");

        // CRUD Endpoints
        group.MapGet("", GetTasks)
            .WithName("GetTasks")
            .WithDescription(@"Retrieve tasks with pagination and optional filtering.

**Query Parameters:**
- `status`: Filter by task status (Pending, Classifying, InProgress, Completed, Failed, Cancelled)
- `type`: Filter by task type (BugFix, Feature, Refactor, Documentation, Test, Deployment)
- `page`: Page number (default: 1, min: 1)
- `pageSize`: Items per page (default: 50, max: 100)

**Response Headers:**
- `X-Total-Count`: Total number of items
- `X-Page-Number`: Current page number
- `X-Page-Size`: Items per page
- `X-Total-Pages`: Total number of pages
- `Link`: HATEOAS pagination links")
            .WithSummary("List tasks with pagination and filtering")
            .Produces<List<TaskDto>>(StatusCodes.Status200OK, "application/json");

        group.MapGet("{id:guid}", GetTask)
            .WithName("GetTask")
            .WithDescription(@"Retrieve a specific task by its unique identifier.

**Returns:**
- Task details including all executions
- 404 if task not found")
            .WithSummary("Get task by ID")
            .Produces<TaskDetailDto>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateTask)
            .WithName("CreateTask")
            .WithDescription(@"Create a new coding task.

**Validation Rules:**
- Title: 1-200 characters (required)
- Description: 1-10,000 characters (required)

**Response:**
- 201 Created with Location header pointing to the new task
- Task is created in 'Pending' status")
            .WithSummary("Create a new task")
            .Produces<TaskDto>(StatusCodes.Status201Created, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPut("{id:guid}", UpdateTask)
            .WithName("UpdateTask")
            .WithDescription(@"Update an existing task's title and description.

**Validation Rules:**
- Title: 1-200 characters (required)
- Description: 1-10,000 characters (required)

**Restrictions:**
- Cannot update tasks that are currently in progress
- Returns 400 if task is in InProgress status
- Returns 404 if task not found")
            .WithSummary("Update task details")
            .Produces<TaskDto>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("{id:guid}", DeleteTask)
            .WithName("DeleteTask")
            .WithDescription(@"Delete a task by its unique identifier.

**Restrictions:**
- Cannot delete tasks that are currently in progress
- Returns 400 if task is in InProgress status
- Returns 404 if task not found")
            .WithSummary("Delete task")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        // Execution Endpoints
        group.MapPost("{id:guid}/execute", ExecuteTask)
            .WithName("ExecuteTask")
            .WithDescription(@"Execute a task using the specified or auto-selected execution strategy.

**Strategy Selection:**
- If not specified, strategy is automatically selected based on task complexity:
  - Simple → SingleShot
  - Medium → Iterative
  - Complex → MultiAgent
  - Epic → HybridExecution

**Rate Limiting:**
- Maximum 10 executions per hour per user
- Returns 429 Too Many Requests if limit exceeded

**Response:**
- 202 Accepted with Location header pointing to executions endpoint
- Execution is queued and will be processed asynchronously")
            .WithSummary("Execute task")
            .Produces<ExecuteTaskResponse>(StatusCodes.Status202Accepted, "application/json")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("{id:guid}/executions", GetTaskExecutions)
            .WithName("GetTaskExecutions")
            .WithDescription(@"Get execution history for a specific task.

**Returns:**
- List of all executions for the task
- Empty list if no executions exist
- Includes execution status, strategy used, tokens, cost, and errors")
            .WithSummary("List task executions")
            .Produces<List<ExecutionDto>>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status404NotFound);

        // SSE logs endpoint for real-time execution logs
        group.MapGet("{id:guid}/logs", StreamTaskLogs)
            .WithName("StreamTaskLogs")
            .WithDescription(@"Stream server-sent events (SSE) for a task's latest or specific execution.

Query Parameters:
- executionId (optional): When provided, streams logs for that execution; otherwise streams the latest execution.

Response:
- text/event-stream with `data: <message>` lines and occasional keepalive pings
")
            .WithSummary("Stream task logs (SSE)")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .Produces(StatusCodes.Status404NotFound);

        // Ping endpoint for health verification
        group.MapGet("/ping", () => Results.Ok(new
        {
            service = "Orchestration Service",
            version = "2.0.0",
            status = "healthy",
            timestamp = DateTime.UtcNow
        }))
        .WithName("PingTasks")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetTasks(
        ITaskRepository repository,
        ILogger<Program> logger,
        HttpContext httpContext,
        TaskStatus? status = null,
        TaskType? type = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        // TODO: Replace with authenticated user when auth is wired
        var userId = Guid.Parse(DefaultUserId);

        logger.LogInformation(
            "Getting tasks (page: {Page}, pageSize: {PageSize}, status: {Status}, type: {Type})",
            page, pageSize, status, type);

        var pagination = new PaginationParameters(page, pageSize);
        var pagedResult = await repository.GetPagedByUserIdAsync(userId, pagination, status, type, ct);

        var items = pagedResult.Items.Select(MapToTaskDto).ToList();

        // Add pagination metadata to response headers
        httpContext.Response.Headers["X-Total-Count"] = pagedResult.TotalCount.ToString();
        httpContext.Response.Headers["X-Page-Number"] = pagedResult.PageNumber.ToString();
        httpContext.Response.Headers["X-Page-Size"] = pagedResult.PageSize.ToString();
        httpContext.Response.Headers["X-Total-Pages"] = pagedResult.TotalPages.ToString();

        // Add HATEOAS links
        AddPaginationLinks(httpContext, pagedResult, pageSize);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetTask(
        Guid id,
        ITaskRepository repository,
        IExecutionRepository executionRepository,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting task {TaskId}", id);

        var task = await repository.GetWithExecutionsAsync(id, ct);
        if (task is null)
        {
            return Results.NotFound();
        }

        var dto = new TaskDetailDto
        {
            Id = task.Id,
            UserId = task.UserId,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            Complexity = task.Complexity,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            Executions = task.Executions.Select(e => new ExecutionDto
            {
                Id = e.Id,
                TaskId = e.TaskId,
                Strategy = e.Strategy,
                ModelUsed = e.ModelUsed,
                Status = e.Status,
                ErrorMessage = e.ErrorMessage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                TokensUsed = e.Result?.TokensUsed ?? 0,
                CostUSD = e.Result?.CostUSD ?? 0
            }).ToList()
        };

        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskRequest request,
        IValidator<CreateTaskRequest> validator,
        ITaskService taskService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        logger.LogInformation("Creating task: {Title}", request.Title);

        // TODO: Replace with authenticated user when auth is wired
        var userId = Guid.Parse(DefaultUserId);

        var task = await taskService.CreateTaskAsync(userId, request.Title, request.Description, ct);
        var dto = MapToTaskDto(task);

        return Results.Created($"/tasks/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateTask(
        Guid id,
        UpdateTaskRequest request,
        IValidator<UpdateTaskRequest> validator,
        ITaskService taskService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        logger.LogInformation("Updating task {TaskId}: {Title}", id, request.Title);

        try
        {
            var task = await taskService.UpdateTaskAsync(id, request.Title, request.Description, ct);
            var dto = MapToTaskDto(task);
            return Results.Ok(dto);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Task {TaskId} not found", id);
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            // Domain throws InvalidOperationException when updating while InProgress
            logger.LogWarning(ex, "Cannot update task {TaskId}", id);
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteTask(
        Guid id,
        ITaskService taskService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Deleting task {TaskId}", id);

        try
        {
            await taskService.DeleteTaskAsync(id, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Task {TaskId} not found", id);
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot delete task {TaskId}", id);
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ExecuteTask(
        Guid id,
        ExecuteTaskRequest request,
        IValidator<ExecuteTaskRequest> validator,
        ITaskRepository repository,
        IExecutionCoordinator coordinator,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        logger.LogInformation("Executing task {TaskId}", id);

        var task = await repository.GetByIdAsync(id, ct);
        if (task is null)
        {
            return Results.NotFound();
        }
        // Kick off background execution via coordinator
        var overrideStrategyName = request.Strategy?.ToString();
        var execution = await coordinator.QueueExecutionAsync(task, overrideStrategyName, ct);

        var response = new ExecuteTaskResponse
        {
            TaskId = id,
            ExecutionId = execution.Id,
            Strategy = execution.Strategy,
            Message = "Task execution queued. Use GET /tasks/{id}/executions or /tasks/{id}/logs to monitor progress."
        };

        return Results.AcceptedAtRoute(
            "GetTaskExecutions",
            new { id },
            response);
    }

    private static async Task<IResult> GetTaskExecutions(
        Guid id,
        IExecutionRepository executionRepository,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting executions for task {TaskId}", id);

        var executions = await executionRepository.GetByTaskIdAsync(id, ct);

        var dtos = executions.Select(e => new ExecutionDto
        {
            Id = e.Id,
            TaskId = e.TaskId,
            Strategy = e.Strategy,
            ModelUsed = e.ModelUsed,
            Status = e.Status,
            ErrorMessage = e.ErrorMessage,
            StartedAt = e.StartedAt,
            CompletedAt = e.CompletedAt,
            TokensUsed = e.Result?.TokensUsed ?? 0,
            CostUSD = e.Result?.CostUSD ?? 0
        }).ToList();

        return Results.Ok(dtos);
    }

    // Helper methods

    private static TaskDto MapToTaskDto(CodingTask task)
    {
        return new TaskDto
        {
            Id = task.Id,
            UserId = task.UserId,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            Complexity = task.Complexity,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            ExecutionCount = task.Executions.Count
        };
    }

    private static void AddPaginationLinks(HttpContext httpContext, PagedResult<CodingTask> pagedResult, int pageSize)
    {
        var request = httpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}{request.Path}";

        // Preserve existing query parameters (e.g., status, type) except page and pageSize
        var preserved = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in request.Query)
        {
            if (!kvp.Key.Equals("page", StringComparison.OrdinalIgnoreCase)
                && !kvp.Key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
            {
                preserved[kvp.Key] = kvp.Value;
            }
        }

        string BuildLink(int targetPage, string rel)
        {
            var qs = QueryString.Create(preserved)
                .Add("page", targetPage.ToString())
                .Add("pageSize", pageSize.ToString());

            return $"<{baseUrl}{qs.ToUriComponent()}>; rel=\"{rel}\"";
        }

        var links = new List<string>
        {
            BuildLink(1, "first")
        };

        if (pagedResult.TotalPages > 0)
        {
            links.Add(BuildLink(pagedResult.TotalPages, "last"));
        }

        if (pagedResult.HasPreviousPage)
        {
            links.Add(BuildLink(pagedResult.PageNumber - 1, "prev"));
        }

        if (pagedResult.HasNextPage)
        {
            links.Add(BuildLink(pagedResult.PageNumber + 1, "next"));
        }

        if (links.Count > 0)
        {
            httpContext.Response.Headers["Link"] = string.Join(", ", links);
        }
    }

    private static ExecutionStrategy SelectStrategyForComplexity(TaskComplexity complexity)
    {
        return complexity switch
        {
            TaskComplexity.Simple => ExecutionStrategy.SingleShot,
            TaskComplexity.Medium => ExecutionStrategy.Iterative,
            TaskComplexity.Complex => ExecutionStrategy.MultiAgent,
            TaskComplexity.Epic => ExecutionStrategy.HybridExecution,
            _ => ExecutionStrategy.Iterative
        };
    }

    private static async Task<IResult> StreamTaskLogs(
        Guid id,
        HttpContext httpContext,
        IExecutionRepository executionRepository,
        IExecutionLogService logService,
        ILogger<Program> logger,
        Guid? executionId,
        CancellationToken ct)
    {
        // Find the execution to stream
        Guid executionToStream = executionId ??
            (await executionRepository.GetLatestByTaskIdAsync(id, ct))?.Id ?? Guid.Empty;

        if (executionToStream == Guid.Empty)
        {
            return Results.NotFound();
        }

        httpContext.Response.Headers["Cache-Control"] = "no-cache";
        httpContext.Response.Headers["Content-Type"] = "text/event-stream";
        httpContext.Response.Headers["X-Accel-Buffering"] = "no"; // Nginx buffering off

        await foreach (var line in logService.ReadStreamAsync(executionToStream, ct))
        {
            await httpContext.Response.WriteAsync($"data: {line}\n\n", ct);
            await httpContext.Response.Body.FlushAsync(ct);
        }

        return Results.Ok();
    }
}
