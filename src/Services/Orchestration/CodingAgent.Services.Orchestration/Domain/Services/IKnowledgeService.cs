namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Service for knowledge retrieval with vector search (RAG)
/// </summary>
public interface IKnowledgeService
{
    Task IndexKnowledgeAsync(KnowledgeItem item, CancellationToken ct);
    Task<IEnumerable<KnowledgeItem>> SearchKnowledgeAsync(string query, float threshold, int limit, CancellationToken ct);
    Task<KnowledgeContext> BuildContextAsync(string query, CancellationToken ct);
}

/// <summary>
/// Knowledge item for indexing
/// </summary>
public class KnowledgeItem
{
    public Guid Id { get; set; }
    public string ContentType { get; set; } = string.Empty; // "code", "documentation", "error_message"
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Knowledge context for RAG
/// </summary>
public class KnowledgeContext
{
    public IEnumerable<KnowledgeItem> RelevantItems { get; set; } = Array.Empty<KnowledgeItem>();
    public Dictionary<Guid, float> RelevanceScores { get; set; } = new();
    public string SummarizedContext { get; set; } = string.Empty;
}

