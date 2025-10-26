using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingAgent.Services.Ollama.Domain.ValueObjects;

namespace CodingAgent.Services.Ollama.Infrastructure.Http;

/// <summary>
/// HTTP client implementation for Ollama REST API
/// </summary>
public class OllamaHttpClient : IOllamaHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaHttpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OllamaHttpClient(HttpClient httpClient, ILogger<OllamaHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<List<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing models from Ollama backend");

            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(_jsonOptions, cancellationToken);
            
            if (result?.Models == null)
            {
                _logger.LogWarning("No models returned from Ollama backend");
                return new List<OllamaModelInfo>();
            }

            _logger.LogInformation("Found {Count} models in Ollama backend", result.Models.Count);
            return result.Models;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to list models from Ollama backend");
            throw new InvalidOperationException("Could not connect to Ollama backend", ex);
        }
    }

    public async Task<OllamaGenerateResponse> GenerateAsync(
        OllamaGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating text with model {Model}", request.Model);

            var apiRequest = new
            {
                model = request.Model,
                prompt = request.Prompt,
                system = request.System,
                stream = request.Stream,
                options = new
                {
                    temperature = request.Temperature,
                    num_predict = request.MaxTokens
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                apiRequest,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaApiResponse>(_jsonOptions, cancellationToken);
            
            if (result == null)
            {
                throw new InvalidOperationException("Received null response from Ollama");
            }

            _logger.LogInformation(
                "Generated {TokenCount} tokens with model {Model} in {DurationMs}ms",
                result.EvalCount, request.Model, result.TotalDuration / 1_000_000);

            return new OllamaGenerateResponse
            {
                Model = result.Model,
                Response = result.Response,
                PromptEvalCount = result.PromptEvalCount,
                EvalCount = result.EvalCount,
                EvalDuration = result.EvalDuration,
                Done = result.Done,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to generate text with Ollama");
            throw new InvalidOperationException("Generation failed", ex);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama health check failed");
            return false;
        }
    }

    // Internal DTOs for Ollama API responses
    private record OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo> Models { get; init; } = new();
    }

    private record OllamaApiResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("response")]
        public string Response { get; init; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; init; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; init; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; init; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; init; }

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; init; }
    }
}
