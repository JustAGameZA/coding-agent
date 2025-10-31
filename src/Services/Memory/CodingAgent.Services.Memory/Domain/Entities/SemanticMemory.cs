namespace CodingAgent.Services.Memory.Domain.Entities;

/// <summary>
/// Represents semantic memory - vector embeddings of code patterns, solutions, best practices
/// </summary>
public class SemanticMemory
{
    public Guid Id { get; private set; }
    public string ContentType { get; private set; } = string.Empty; // 'code_pattern', 'solution', 'best_practice'
    public string Content { get; private set; } = string.Empty;
    public float[] Embedding { get; private set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public Guid? SourceEpisodeId { get; private set; }
    public float ConfidenceScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private SemanticMemory() { }

    public SemanticMemory(
        string contentType,
        string content,
        float[] embedding,
        Dictionary<string, object>? metadata = null,
        Guid? sourceEpisodeId = null,
        float confidenceScore = 1.0f)
    {
        Id = Guid.NewGuid();
        ContentType = contentType;
        Content = content;
        Embedding = embedding;
        Metadata = metadata ?? new Dictionary<string, object>();
        SourceEpisodeId = sourceEpisodeId;
        ConfidenceScore = confidenceScore;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateConfidence(float newConfidence)
    {
        ConfidenceScore = Math.Clamp(newConfidence, 0.0f, 1.0f);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(Dictionary<string, object> metadata)
    {
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }
}

