using CodingAgent.Services.Ollama.Domain.ValueObjects;

namespace CodingAgent.Services.Ollama.Infrastructure.CloudApi;

/// <summary>
/// Interface for cloud API fallback when Ollama backend is unavailable
/// </summary>
public interface ICloudApiClient
{
    /// <summary>
    /// Checks if the cloud API is properly configured
    /// </summary>
    /// <returns>True if cloud API credentials are configured</returns>
    bool IsConfigured();

    /// <summary>
    /// Checks if tokens are available for this month
    /// </summary>
    /// <returns>True if tokens are available within monthly limit</returns>
    Task<bool> HasTokensAvailableAsync();

    /// <summary>
    /// Generates text using the cloud API (fallback from Ollama)
    /// </summary>
    /// <param name="request">The inference request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated response</returns>
    Task<string> GenerateAsync(InferenceRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for inference (abstracted from Ollama-specific format)
/// </summary>
public record InferenceRequest
{
    /// <summary>
    /// Model name to use for generation
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The prompt to generate from
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// System message (optional)
    /// </summary>
    public string? System { get; init; }

    /// <summary>
    /// Temperature (0.0 - 1.0)
    /// </summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; init; } = 2000;
}
