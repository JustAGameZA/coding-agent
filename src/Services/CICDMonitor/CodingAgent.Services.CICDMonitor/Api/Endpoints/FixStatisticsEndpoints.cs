using CodingAgent.Services.CICDMonitor.Domain.Repositories;

namespace CodingAgent.Services.CICDMonitor.Api.Endpoints;

/// <summary>
/// Endpoints for fix statistics and monitoring.
/// </summary>
public static class FixStatisticsEndpoints
{
    public static void MapFixStatisticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fix-statistics")
            .WithTags("Fix Statistics");

        group.MapGet("", GetOverallStatistics)
            .WithName("GetFixStatistics")
            .WithDescription("Get overall fix attempt statistics")
            .Produces<FixStatistics>();

        group.MapGet("/by-error-pattern", GetStatisticsByErrorPattern)
            .WithName("GetFixStatisticsByErrorPattern")
            .WithDescription("Get fix attempt statistics grouped by error pattern")
            .Produces<Dictionary<string, FixStatistics>>();
    }

    private static async Task<IResult> GetOverallStatistics(
        IFixAttemptRepository repository,
        CancellationToken cancellationToken)
    {
        var statistics = await repository.GetStatisticsAsync(cancellationToken);
        return Results.Ok(statistics);
    }

    private static async Task<IResult> GetStatisticsByErrorPattern(
        IFixAttemptRepository repository,
        CancellationToken cancellationToken)
    {
        var statistics = await repository.GetStatisticsByErrorPatternAsync(cancellationToken);
        return Results.Ok(statistics);
    }
}
