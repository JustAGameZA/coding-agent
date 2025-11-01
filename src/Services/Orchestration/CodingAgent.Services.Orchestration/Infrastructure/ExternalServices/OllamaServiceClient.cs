using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for Ollama service to query available models.
/// </summary>
public class OllamaServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaServiceClient(HttpClient httpClient, ILogger<OllamaServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all available models from Ollama backend.
    /// </summary>
    public async Task<List<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Querying Ollama service for available models");

            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama service returned {StatusCode}, treating as no models available", response.StatusCode);
                return new List<OllamaModelInfo>();
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(JsonOptions, cancellationToken);
            
            if (result?.Models == null)
            {
                _logger.LogWarning("No models returned from Ollama service");
                return new List<OllamaModelInfo>();
            }

            _logger.LogInformation("Found {Count} models from Ollama service", result.Models.Count);
            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query Ollama service for models, treating as no models available");
            return new List<OllamaModelInfo>();
        }
    }

    private class OllamaTagsResponse
    {
        public List<OllamaModelInfo>? Models { get; set; }
    }
}

/// <summary>
/// Model information from Ollama API.
/// </summary>
public class OllamaModelInfo
{
    public required string Name { get; set; }
    public long Size { get; set; }
    public string Digest { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
}

