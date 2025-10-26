using CodingAgent.Services.Ollama.Domain.ValueObjects;

namespace CodingAgent.Services.Ollama.Infrastructure.Http;

/// <summary>
/// HTTP client interface for communicating with Ollama backend
/// </summary>
public interface IOllamaHttpClient
{
    /// <summary>
    /// Lists all available models in the Ollama backend
    /// </summary>
    Task<List<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates text using the specified model
    /// </summary>
    Task<OllamaGenerateResponse> GenerateAsync(
        OllamaGenerateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the Ollama backend is healthy
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Model information returned by Ollama API
/// </summary>
public record OllamaModelInfo
{
    public required string Name { get; init; }
    public long Size { get; init; }
    public string Digest { get; init; } = string.Empty;
    public DateTime ModifiedAt { get; init; }
}
