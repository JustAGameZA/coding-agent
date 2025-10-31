using CodingAgent.Services.Memory.Domain.Services;

namespace CodingAgent.Services.Memory.Infrastructure.Services;

/// <summary>
/// Service for generating embeddings from text content
/// Currently integrates with Ollama service; can be extended to support other providers
/// </summary>
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string content, CancellationToken ct);
    Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> contents, CancellationToken ct);
}

/// <summary>
/// Embedding service implementation using Ollama
/// TODO: Add actual Ollama embedding API integration
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OllamaEmbeddingService> _logger;
    private readonly IConfiguration _configuration;

    public OllamaEmbeddingService(
        IHttpClientFactory httpClientFactory,
        ILogger<OllamaEmbeddingService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string content, CancellationToken ct)
    {
        _logger.LogDebug("Generating embedding for content length {Length}", content.Length);
        
        // TODO: Implement actual Ollama embedding API call
        // For now, return a dummy embedding vector
        // In production, this would call: POST /api/embeddings with { "model": "nomic-embed-text", "prompt": content }
        
        var ollamaUrl = _configuration["Ollama:BaseUrl"] ?? "http://ollama-service:5008";
        var client = _httpClientFactory.CreateClient("ollama");
        
        try
        {
            var request = new
            {
                model = "nomic-embed-text", // or other embedding model
                prompt = content
            };

            var response = await client.PostAsJsonAsync($"{ollamaUrl}/api/embeddings", request, ct);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
                return result?.Embedding ?? GenerateDummyEmbedding(content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding via Ollama, using dummy embedding");
        }
        
        return GenerateDummyEmbedding(content);
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> contents, CancellationToken ct)
    {
        var tasks = contents.Select(c => GenerateEmbeddingAsync(c, ct));
        return await Task.WhenAll(tasks);
    }

    private static float[] GenerateDummyEmbedding(string content)
    {
        // Generate a deterministic dummy embedding based on content hash
        // In production, this should never be used - always use real embeddings
        var hash = content.GetHashCode();
        var random = new Random(hash);
        var embedding = new float[1536]; // Standard embedding dimension
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Random value between -1 and 1
        }
        return embedding;
    }
}

internal class EmbeddingResponse
{
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

