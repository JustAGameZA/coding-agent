using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace CodingAgent.Services.Orchestration.Infrastructure.LLM;

/// <summary>
/// LLM client implementation using Ollama backend.
/// Provides real AI responses via locally-hosted open-source models.
/// </summary>
public class OllamaLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLlmClient> _logger;
    private readonly ActivitySource _activitySource;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaLlmClient(
        HttpClient httpClient,
        ILogger<OllamaLlmClient> logger,
        ActivitySource activitySource)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("OllamaLlmClient.Generate");
        activity?.SetTag("llm.model", request.Model);
        activity?.SetTag("llm.temperature", request.Temperature);
        activity?.SetTag("llm.max_tokens", request.MaxTokens);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Convert LlmRequest (with messages) to Ollama format (prompt + system)
            var systemMessage = request.Messages
                .FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase))?.Content;

            // Combine user messages into a single prompt
            var userMessages = request.Messages
                .Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var prompt = string.Join("\n\n", userMessages.Select(m => $"{m.Role}: {m.Content}"));

            // Map model name to Ollama model if needed
            var ollamaModel = MapModelName(request.Model);

            _logger.LogInformation(
                "Generating response with Ollama model {Model} (requested: {RequestedModel})",
                ollamaModel,
                request.Model);

            // Call Ollama backend API
            var ollamaRequest = new
            {
                model = ollamaModel,
                prompt = prompt,
                system = systemMessage,
                stream = false,
                options = new
                {
                    temperature = (float)request.Temperature,
                    num_predict = request.MaxTokens
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                ollamaRequest,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaApiResponse>(
                JsonOptions,
                cancellationToken);

            if (ollamaResponse == null)
            {
                throw new InvalidOperationException("Received null response from Ollama backend");
            }

            stopwatch.Stop();
            activity?.SetTag("llm.tokens", ollamaResponse.EvalCount);
            activity?.SetTag("llm.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("llm.model_used", ollamaResponse.Model ?? ollamaModel);

            var totalTokens = ollamaResponse.PromptEvalCount + ollamaResponse.EvalCount;
            
            _logger.LogInformation(
                "Generated {TokenCount} tokens with Ollama model {Model} in {DurationMs}ms",
                totalTokens,
                ollamaResponse.Model ?? ollamaModel,
                stopwatch.ElapsedMilliseconds);

            // Ollama is free (on-premise), so cost is 0
            return new LlmResponse
            {
                Content = ollamaResponse.Response ?? string.Empty,
                TokensUsed = totalTokens,
                Cost = 0m, // Free - using local Ollama
                Model = ollamaResponse.Model ?? ollamaModel
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to generate response with Ollama");
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout");
            _logger.LogWarning(
                "Ollama request timed out after {DurationMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Maps generic model names to Ollama-specific models.
    /// Uses available models from the Ollama backend.
    /// Note: In production, this should query ModelRegistry for available models.
    /// </summary>
    private static string MapModelName(string requestedModel)
    {
        // Basic mapping - ModelRegistry should be used for dynamic discovery
        return requestedModel.ToLowerInvariant() switch
        {
            "gpt-4o-mini" => "mistral:latest", // Fast chat model (use :latest tag)
            "gpt-4" or "gpt-4o" => "qwen3-coder:latest", // Better quality
            "gpt-3.5-turbo" => "mistral:latest",
            "claude-3.5-sonnet" => "llama3.2:latest",
            "mistral:7b" => "mistral:latest", // Map :7b to :latest
            _ => requestedModel // Use as-is if not recognized
        };
    }

    /// <summary>
    /// Internal DTO for Ollama API response.
    /// </summary>
    private class OllamaApiResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}

