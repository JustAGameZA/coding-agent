using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

/// <summary>
/// Model management API endpoints
/// </summary>
public static class ModelEndpoints
{
    public static void MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/models").WithTags("Models");

        // Model Registry
        group.MapGet("/", GetAvailableModels)
            .Produces<List<ModelInfo>>();

        group.MapGet("/provider/{provider}", GetModelsByProvider)
            .Produces<List<ModelInfo>>();

        group.MapGet("/refresh", RefreshModels)
            .Produces(StatusCodes.Status200OK);

        group.MapGet("/available/{modelName}", CheckModelAvailable)
            .Produces<bool>();

        // Model Selection
        group.MapPost("/select", SelectBestModel)
            .Produces<ModelSelectionResult>();

        // Performance Metrics
        group.MapGet("/metrics", GetAllMetrics)
            .Produces<Dictionary<string, ModelPerformanceMetrics>>();

        group.MapGet("/metrics/{modelName}", GetModelMetrics)
            .Produces<ModelPerformanceMetrics>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/best/{taskType}/{complexity}", GetBestModel)
            .Produces<string>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<List<ModelInfo>>> GetAvailableModels(
        IModelRegistry modelRegistry,
        CancellationToken ct)
    {
        var models = await modelRegistry.GetAvailableModelsAsync(ct);
        return TypedResults.Ok(models);
    }

    private static async Task<Ok<List<ModelInfo>>> GetModelsByProvider(
        string provider,
        IModelRegistry modelRegistry,
        CancellationToken ct)
    {
        var models = await modelRegistry.GetModelsByProviderAsync(provider, ct);
        return TypedResults.Ok(models);
    }

    private static async Task<Ok> RefreshModels(
        IModelRegistry modelRegistry,
        CancellationToken ct)
    {
        await modelRegistry.RefreshAsync(ct);
        return TypedResults.Ok();
    }

    private static async Task<Ok<bool>> CheckModelAvailable(
        string modelName,
        IModelRegistry modelRegistry,
        CancellationToken ct)
    {
        var isAvailable = await modelRegistry.IsModelAvailableAsync(modelName, ct);
        return TypedResults.Ok(isAvailable);
    }

    private static async Task<Ok<ModelSelectionResult>> SelectBestModel(
        [FromBody] ModelSelectionRequest request,
        IMLModelSelector modelSelector,
        CancellationToken ct)
    {
        var result = await modelSelector.SelectBestModelAsync(
            request.TaskDescription,
            request.TaskType,
            request.Complexity,
            request.UserId,
            ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<Dictionary<string, ModelPerformanceMetrics>>> GetAllMetrics(
        IModelPerformanceTracker tracker,
        CancellationToken ct)
    {
        var metrics = await tracker.GetAllMetricsAsync(ct);
        return TypedResults.Ok(metrics);
    }

    private static async Task<Results<Ok<ModelPerformanceMetrics>, NotFound>> GetModelMetrics(
        string modelName,
        IModelPerformanceTracker tracker,
        CancellationToken ct)
    {
        var metrics = await tracker.GetMetricsAsync(modelName, ct);
        if (metrics == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(metrics);
    }

    private static async Task<Results<Ok<string>, NotFound>> GetBestModel(
        string taskType,
        string complexity,
        IModelPerformanceTracker tracker,
        CancellationToken ct)
    {
        if (!Enum.TryParse<TaskComplexity>(complexity, ignoreCase: true, out var complexityEnum))
        {
            return TypedResults.NotFound();
        }
        
        var bestModel = await tracker.GetBestModelAsync(taskType, complexityEnum, ct);
        if (bestModel == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(bestModel);
    }
}

public record ModelSelectionRequest
{
    public required string TaskDescription { get; init; }
    public required string TaskType { get; init; }
    public required TaskComplexity Complexity { get; init; }
    public Guid? UserId { get; init; }
}

