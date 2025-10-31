using CodingAgent.Services.Memory.Domain.Entities;
using CodingAgent.Services.Memory.Domain.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CodingAgent.Services.Memory.Api.Endpoints;

/// <summary>
/// Memory Service API endpoints
/// </summary>
public static class MemoryEndpoints
{
    public static void MapMemoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/memory").WithTags("Memory");

        // Episodic Memory
        group.MapPost("/episodes", CreateEpisode)
            .Produces<Episode>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/episodes/{id:guid}", GetEpisode)
            .Produces<Episode>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/episodes/task/{taskId:guid}", GetEpisodesByTask)
            .Produces<IEnumerable<Episode>>();

        group.MapGet("/episodes/similar", SearchSimilarEpisodes)
            .Produces<IEnumerable<Episode>>();

        // Semantic Memory
        group.MapPost("/semantic", StoreSemanticMemory)
            .Produces<SemanticMemory>(StatusCodes.Status201Created);

        group.MapGet("/semantic/search", SearchSemanticMemory)
            .Produces<IEnumerable<SemanticMemory>>();

        // Procedural Memory
        group.MapPost("/procedures", StoreProcedure)
            .Produces<Procedure>(StatusCodes.Status201Created);

        group.MapGet("/procedures/context", GetProcedureByContext)
            .Produces<Procedure>()
            .Produces(StatusCodes.Status404NotFound);

        // RAG Context Retrieval
        group.MapGet("/context", GetMemoryContext)
            .Produces<MemoryContext>();
    }

    private static async Task<Created<Episode>> CreateEpisode(
        [FromBody] CreateEpisodeRequest request,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var episode = new Episode(
            request.TaskId,
            request.ExecutionId,
            request.UserId,
            request.Timestamp,
            request.EventType,
            request.Context,
            request.Outcome,
            request.LearnedPatterns);

        var created = await memoryService.RecordEpisodeAsync(episode, ct);
        return TypedResults.Created($"/api/memory/episodes/{created.Id}", created);
    }

    private static async Task<Results<Ok<Episode>, NotFound>> GetEpisode(
        Guid id,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        // Implementation would use repository directly
        // Simplified for now
        return TypedResults.NotFound();
    }

    private static async Task<Ok<IEnumerable<Episode>>> GetEpisodesByTask(
        Guid taskId,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var episodes = await memoryService.RetrieveSimilarEpisodesAsync(
            taskId.ToString(), 100, ct);
        
        // Filter by task ID in implementation
        return TypedResults.Ok(episodes.Where(e => e.TaskId == taskId));
    }

    private static async Task<Ok<IEnumerable<Episode>>> SearchSimilarEpisodes(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var episodes = await memoryService.RetrieveSimilarEpisodesAsync(query, limit, ct);
        return TypedResults.Ok(episodes);
    }

    private static async Task<Created<SemanticMemory>> StoreSemanticMemory(
        [FromBody] CreateSemanticMemoryRequest request,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var memory = new SemanticMemory(
            request.ContentType,
            request.Content,
            request.Embedding,
            request.Metadata,
            request.SourceEpisodeId,
            request.ConfidenceScore);

        var created = await memoryService.StoreSemanticMemoryAsync(memory, ct);
        return TypedResults.Created($"/api/memory/semantic/{created.Id}", created);
    }

    private static async Task<Ok<IEnumerable<SemanticMemory>>> SearchSemanticMemory(
        [FromQuery] string query,
        [FromQuery] float threshold = 0.7f,
        [FromQuery] int limit = 10,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var results = await memoryService.SearchSemanticMemoryAsync(query, threshold, limit, ct);
        return TypedResults.Ok(results);
    }

    private static async Task<Created<Procedure>> StoreProcedure(
        [FromBody] CreateProcedureRequest request,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var procedure = new Procedure(
            request.ProcedureName,
            request.Description,
            request.ContextPattern,
            request.Steps);

        var created = await memoryService.StoreProcedureAsync(procedure, ct);
        return TypedResults.Created($"/api/memory/procedures/{created.Id}", created);
    }

    private static async Task<Results<Ok<Procedure>, NotFound>> GetProcedureByContext(
        [FromQuery] Dictionary<string, object> context,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var procedure = await memoryService.RetrieveProcedureAsync(context, ct);
        if (procedure == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(procedure);
    }

    private static async Task<Ok<MemoryContext>> GetMemoryContext(
        [FromQuery] string query,
        [FromQuery] int episodeLimit = 5,
        [FromQuery] int semanticLimit = 10,
        IMemoryService memoryService,
        CancellationToken ct)
    {
        var context = await memoryService.RetrieveContextAsync(query, episodeLimit, semanticLimit, ct);
        return TypedResults.Ok(context);
    }
}

// Request DTOs
public record CreateEpisodeRequest(
    Guid? TaskId,
    Guid? ExecutionId,
    Guid UserId,
    DateTime Timestamp,
    string EventType,
    Dictionary<string, object> Context,
    Dictionary<string, object> Outcome,
    List<string>? LearnedPatterns);

public record CreateSemanticMemoryRequest(
    string ContentType,
    string Content,
    float[] Embedding,
    Dictionary<string, object>? Metadata,
    Guid? SourceEpisodeId,
    float ConfidenceScore = 1.0f);

public record CreateProcedureRequest(
    string ProcedureName,
    string Description,
    Dictionary<string, object> ContextPattern,
    List<ProcedureStep> Steps);

