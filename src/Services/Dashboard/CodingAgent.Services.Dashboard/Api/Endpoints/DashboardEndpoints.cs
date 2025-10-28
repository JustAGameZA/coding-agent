using System.Diagnostics;
using CodingAgent.Services.Dashboard.Domain.Services;

namespace CodingAgent.Services.Dashboard.Api.Endpoints;

/// <summary>
/// Dashboard aggregation endpoints
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard")
            .WithTags("Dashboard")
            .WithOpenApi();

        group.MapGet("/stats", GetStats)
            .WithName("GetDashboardStats")
            .WithDescription("Get aggregated statistics from all services. Data is cached for 5 minutes.")
            .WithSummary("Get dashboard statistics");

        group.MapGet("/tasks", GetTasks)
            .WithName("GetDashboardTasks")
            .WithDescription("Get enriched tasks with execution data. Supports pagination.")
            .WithSummary("Get enriched tasks");

        group.MapGet("/activity", GetActivity)
            .WithName("GetDashboardActivity")
            .WithDescription("Get recent activity events from all services. Data is cached for 5 minutes.")
            .WithSummary("Get recent activity");
    }

    private static async Task<IResult> GetStats(
        IDashboardAggregationService service,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetDashboardStats");

        try
        {
            logger.LogInformation("Retrieving dashboard stats");
            var stats = await service.GetStatsAsync(ct);
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve dashboard stats");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem("Failed to retrieve dashboard statistics");
        }
    }

    private static async Task<IResult> GetTasks(
        IDashboardAggregationService service,
        ILogger<Program> logger,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetDashboardTasks");
        activity?.SetTag("page", page);
        activity?.SetTag("pageSize", pageSize);

        try
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1 || pageSize > 100)
            {
                pageSize = 20;
            }

            logger.LogInformation("Retrieving dashboard tasks (page {Page}, size {PageSize})", page, pageSize);
            var tasks = await service.GetTasksAsync(page, pageSize, ct);
            return Results.Ok(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve dashboard tasks");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem("Failed to retrieve tasks");
        }
    }

    private static async Task<IResult> GetActivity(
        IDashboardAggregationService service,
        ILogger<Program> logger,
        int limit = 50,
        CancellationToken ct = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetDashboardActivity");
        activity?.SetTag("limit", limit);

        try
        {
            if (limit < 1 || limit > 100)
            {
                limit = 50;
            }

            logger.LogInformation("Retrieving dashboard activity (limit {Limit})", limit);
            var events = await service.GetActivityAsync(limit, ct);
            return Results.Ok(events);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve dashboard activity");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Results.Problem("Failed to retrieve activity");
        }
    }
}
