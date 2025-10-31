using CodingAgent.Services.Memory.Domain.Entities;
using CodingAgent.Services.Memory.Domain.Repositories;
using CodingAgent.Services.Memory.Domain.Services;
using CodingAgent.Services.Memory.Infrastructure.Services;

namespace CodingAgent.Services.Memory.Application.Services;

/// <summary>
/// High-level memory service implementation for agentic AI operations
/// </summary>
public class MemoryService : IMemoryService
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly ISemanticMemoryRepository _semanticMemoryRepository;
    private readonly IProcedureRepository _procedureRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        IEpisodeRepository episodeRepository,
        ISemanticMemoryRepository semanticMemoryRepository,
        IProcedureRepository procedureRepository,
        IEmbeddingService embeddingService,
        ILogger<MemoryService> logger)
    {
        _episodeRepository = episodeRepository;
        _semanticMemoryRepository = semanticMemoryRepository;
        _procedureRepository = procedureRepository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<Episode> RecordEpisodeAsync(Episode episode, CancellationToken ct)
    {
        _logger.LogInformation("Recording episode {EpisodeId} for task {TaskId}", episode.Id, episode.TaskId);
        return await _episodeRepository.AddAsync(episode, ct);
    }

    public async Task<IEnumerable<Episode>> RetrieveSimilarEpisodesAsync(string query, int limit, CancellationToken ct)
    {
        _logger.LogDebug("Retrieving similar episodes for query: {Query}", query);
        return await _episodeRepository.GetSimilarAsync(query, limit, ct);
    }

    public async Task<SemanticMemory> StoreSemanticMemoryAsync(SemanticMemory memory, CancellationToken ct)
    {
        _logger.LogInformation("Storing semantic memory {MemoryId} of type {ContentType}", memory.Id, memory.ContentType);
        return await _semanticMemoryRepository.AddAsync(memory, ct);
    }

    public async Task<IEnumerable<SemanticMemory>> SearchSemanticMemoryAsync(
        string query,
        float threshold,
        int limit,
        CancellationToken ct)
    {
        _logger.LogDebug("Searching semantic memory for query: {Query}", query);
        
        // Generate embedding for query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        
        // Search using vector similarity
        return await _semanticMemoryRepository.SearchAsync(queryEmbedding, threshold, limit, ct);
    }

    public async Task<Procedure> StoreProcedureAsync(Procedure procedure, CancellationToken ct)
    {
        _logger.LogInformation("Storing procedure {ProcedureName}", procedure.ProcedureName);
        return await _procedureRepository.AddAsync(procedure, ct);
    }

    public async Task<Procedure?> RetrieveProcedureAsync(
        Dictionary<string, object> context,
        CancellationToken ct)
    {
        _logger.LogDebug("Retrieving procedure for context");
        return await _procedureRepository.GetByContextAsync(context, ct);
    }

    public async Task UpdateProcedureSuccessRateAsync(
        Guid procedureId,
        bool success,
        TimeSpan executionTime,
        CancellationToken ct)
    {
        _logger.LogDebug("Updating procedure {ProcedureId} success rate: {Success}", procedureId, success);
        
        var procedure = await _procedureRepository.GetByIdAsync(procedureId, ct);
        if (procedure == null)
        {
            _logger.LogWarning("Procedure {ProcedureId} not found for update", procedureId);
            return;
        }

        procedure.RecordExecution(success, executionTime);
        await _procedureRepository.UpdateAsync(procedure, ct);
    }

    public async Task<MemoryContext> RetrieveContextAsync(
        string query,
        int episodeLimit,
        int semanticLimit,
        CancellationToken ct)
    {
        _logger.LogDebug("Retrieving context for query: {Query}", query);
        
        // Generate embedding for query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        
        // Retrieve episodic knowledge
        var episodes = await _episodeRepository.GetSimilarAsync(query, episodeLimit, ct);
        
        // Retrieve semantic knowledge
        var semanticMemories = await _semanticMemoryRepository.SearchAsync(queryEmbedding, 0.7f, semanticLimit, ct);
        
        // Retrieve relevant procedures (simplified - match by context pattern in real implementation)
        var topProcedures = await _procedureRepository.GetTopProceduresAsync(5, ct);
        
        // Calculate relevance scores (simplified)
        var relevanceScores = new Dictionary<Guid, float>();
        foreach (var episode in episodes)
        {
            relevanceScores[episode.Id] = 0.8f; // Default relevance
        }
        foreach (var memory in semanticMemories)
        {
            relevanceScores[memory.Id] = memory.ConfidenceScore;
        }
        
        return new MemoryContext
        {
            EpisodicKnowledge = episodes,
            SemanticKnowledge = semanticMemories,
            RelevantProcedures = topProcedures,
            RelevanceScores = relevanceScores
        };
    }
}

