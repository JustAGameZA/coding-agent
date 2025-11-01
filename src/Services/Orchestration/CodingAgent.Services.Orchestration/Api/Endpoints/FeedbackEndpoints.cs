using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Feedback = CodingAgent.Services.Orchestration.Domain.Services.Feedback;
using FeedbackType = CodingAgent.Services.Orchestration.Domain.Services.FeedbackType;
using FeedbackAnalysis = CodingAgent.Services.Orchestration.Domain.Services.FeedbackAnalysis;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

/// <summary>
/// Feedback API endpoints for continuous learning
/// </summary>
public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feedback").WithTags("Feedback");

        group.MapPost("/", RecordFeedback)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/task/{taskId:guid}/analysis", GetFeedbackAnalysis)
            .Produces<FeedbackAnalysis>();

        group.MapPost("/task/{taskId:guid}/update-models", UpdateModelsFromFeedback)
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<Created<Feedback>> RecordFeedback(
        [FromBody] RecordFeedbackRequest request,
        IFeedbackService feedbackService,
        CancellationToken ct)
    {
        var feedback = new Feedback
        {
            TaskId = request.TaskId,
            ExecutionId = request.ExecutionId,
            UserId = request.UserId,
            Type = request.Type,
            Rating = request.Rating,
            Reason = request.Reason,
            Context = request.Context ?? new Dictionary<string, object>(),
            ProcedureId = request.ProcedureId
        };

        await feedbackService.RecordFeedbackAsync(feedback, ct);
        return TypedResults.Created($"/api/feedback/{feedback.Id}", feedback);
    }

    private static async Task<Ok<FeedbackAnalysis>> GetFeedbackAnalysis(
        Guid taskId,
        IFeedbackService feedbackService,
        CancellationToken ct)
    {
        var analysis = await feedbackService.AnalyzeFeedbackPatternsAsync(taskId, ct);
        return TypedResults.Ok(analysis);
    }

    private static async Task<Ok> UpdateModelsFromFeedback(
        Guid taskId,
        IFeedbackService feedbackService,
        CancellationToken ct)
    {
        var analysis = await feedbackService.AnalyzeFeedbackPatternsAsync(taskId, ct);
        await feedbackService.UpdateModelParametersAsync(analysis, ct);
        return TypedResults.Ok();
    }
}

// Request DTOs
public record RecordFeedbackRequest(
    Guid TaskId,
    Guid? ExecutionId,
    Guid UserId,
    FeedbackType Type,
    float Rating,
    string? Reason,
    Dictionary<string, object>? Context,
    Guid? ProcedureId);

