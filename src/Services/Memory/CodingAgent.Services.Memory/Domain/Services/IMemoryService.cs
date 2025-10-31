using CodingAgent.Services.Memory.Domain.Entities;

namespace CodingAgent.Services.Memory.Domain.Services;

/// <summary>
/// High-level memory service interface for agentic AI operations
/// </summary>
public interface IMemoryService
{
    // Episodic Memory
    Task<Episode> RecordEpisodeAsync(Episode episode, CancellationToken ct);
    Task<IEnumerable<Episode>> RetrieveSimilarEpisodesAsync(string query, int limit, CancellationToken ct);
    
    // Semantic Memory
    Task<SemanticMemory> StoreSemanticMemoryAsync(SemanticMemory memory, CancellationToken ct);
    Task<IEnumerable<SemanticMemory>> SearchSemanticMemoryAsync(string query, float threshold, int limit, CancellationToken ct);
    
    // Procedural Memory
    Task<Procedure> StoreProcedureAsync(Procedure procedure, CancellationToken ct);
    Task<Procedure?> RetrieveProcedureAsync(Dictionary<string, object> context, CancellationToken ct);
    Task UpdateProcedureSuccessRateAsync(Guid procedureId, bool success, TimeSpan executionTime, CancellationToken ct);
    
    // Memory Retrieval (RAG)
    Task<MemoryContext> RetrieveContextAsync(string query, int episodeLimit, int semanticLimit, CancellationToken ct);
}

/// <summary>
/// Combined context from multiple memory types for RAG
/// </summary>
public class MemoryContext
{
    public IEnumerable<Episode> EpisodicKnowledge { get; set; } = Array.Empty<Episode>();
    public IEnumerable<SemanticMemory> SemanticKnowledge { get; set; } = Array.Empty<SemanticMemory>();
    public IEnumerable<Procedure> RelevantProcedures { get; set; } = Array.Empty<Procedure>();
    public Dictionary<Guid, float> RelevanceScores { get; set; } = new();
}

