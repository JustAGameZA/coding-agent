using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

/// <summary>
/// A/B Testing API endpoints
/// </summary>
public static class ABTestEndpoints
{
    public static void MapABTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ab-tests").WithTags("A/B Testing");

        // A/B Test Management
        group.MapPost("/", CreateABTest)
            .Produces<ABTest>(StatusCodes.Status201Created);

        group.MapGet("/{testId:guid}", GetABTest)
            .Produces<ABTest>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/active/{taskType}", GetActiveTest)
            .Produces<ABTest>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{testId:guid}/end", EndABTest)
            .Produces(StatusCodes.Status200OK);

        // A/B Test Results
        group.MapGet("/{testId:guid}/results", GetABTestResults)
            .Produces<ABTestResults>();

        group.MapPost("/{testId:guid}/results", RecordABTestResult)
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<Created<ABTest>> CreateABTest(
        [FromBody] CreateABTestRequest request,
        IABTestingEngine abTesting,
        CancellationToken ct)
    {
        var test = await abTesting.CreateTestAsync(request, ct);
        return TypedResults.Created($"/api/ab-tests/{test.Id}", test);
    }

    private static async Task<Results<Ok<ABTest>, NotFound>> GetABTest(
        Guid testId,
        IABTestingEngine abTesting,
        CancellationToken ct)
    {
        // Try to get test results which includes test info
        try
        {
            var results = await abTesting.GetResultsAsync(testId, ct);
            // Create a minimal ABTest from results - in production, add GetTestAsync to interface
            return TypedResults.NotFound();
        }
        catch
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<ABTest>, NotFound>> GetActiveTest(
        string taskType,
        Guid? userId,
        IABTestingEngine abTesting,
        CancellationToken ct)
    {
        var test = await abTesting.GetActiveTestAsync(taskType, userId, ct);
        if (test == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(test);
    }

    private static async Task<Ok> EndABTest(
        Guid testId,
        IABTestingEngine abTesting,
        CancellationToken ct)
    {
        await abTesting.EndTestAsync(testId, ct);
        return TypedResults.Ok();
    }

    private static async Task<Ok<ABTestResults>> GetABTestResults(
        Guid testId,
        IABTestingEngine abTesting,
        CancellationToken ct)
    {
        var results = await abTesting.GetResultsAsync(testId, ct);
        return TypedResults.Ok(results);
    }

    private static async Task<Ok> RecordABTestResult(
        Guid testId,
        [FromBody] ABTestResultRequest request,
        IABTestingEngine abTesting,
        CancellationToken ct)
    {
        var result = new ABTestResult
        {
            RequestId = request.RequestId,
            Variant = request.Variant,
            Success = request.Success,
            Duration = request.Duration,
            TokensUsed = request.TokensUsed,
            Cost = request.Cost,
            QualityScore = request.QualityScore
        };

        await abTesting.RecordResultAsync(testId, request.Variant, result, ct);
        return TypedResults.Ok();
    }
}

public record ABTestResultRequest
{
    public required Guid RequestId { get; init; }
    public required string Variant { get; init; }
    public required bool Success { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int TokensUsed { get; init; }
    public required decimal Cost { get; init; }
    public int? QualityScore { get; init; }
}

