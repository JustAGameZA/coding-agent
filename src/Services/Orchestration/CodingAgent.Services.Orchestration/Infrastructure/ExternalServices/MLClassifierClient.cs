using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for ML Classifier service.
/// Handles classification requests with retry and timeout policies.
/// </summary>
public class MLClassifierClient : IMLClassifierClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MLClassifierClient> _logger;
    private readonly ActivitySource _activitySource;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MLClassifierClient(
        HttpClient httpClient,
        ILogger<MLClassifierClient> logger,
        ActivitySource activitySource)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    public async Task<ClassificationResponse> ClassifyAsync(
        ClassificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.TaskDescription))
        {
            throw new ArgumentException("Task description cannot be empty", nameof(request));
        }

        using var activity = _activitySource.StartActivity("MLClassifier.Classify");
        activity?.SetTag("task.description.length", request.TaskDescription.Length);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Calling ML Classifier service for task classification: {TaskDescription}",
                request.TaskDescription.Length > 50 
                    ? request.TaskDescription[..50] + "..." 
                    : request.TaskDescription);

            var response = await _httpClient.PostAsJsonAsync(
                "/classify",
                request,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ClassificationResponse>(
                JsonOptions,
                cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("ML Classifier returned null response");
            }

            stopwatch.Stop();
            
            activity?.SetTag("classification.complexity", result.Complexity);
            activity?.SetTag("classification.confidence", result.Confidence);
            activity?.SetTag("classification.strategy", result.SuggestedStrategy);
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "ML Classification completed: complexity={Complexity}, confidence={Confidence:F2}, " +
                "strategy={Strategy}, duration={Duration}ms",
                result.Complexity,
                result.Confidence,
                result.SuggestedStrategy,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "ML Classifier service request failed after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "ML Classifier service request timed out after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw new HttpRequestException("ML Classifier service request timed out", ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Unexpected error calling ML Classifier service after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = _activitySource.StartActivity("MLClassifier.HealthCheck");
            
            _logger.LogDebug("Checking ML Classifier service availability");

            var response = await _httpClient.GetAsync("/health", cancellationToken);
            var isAvailable = response.IsSuccessStatusCode;

            activity?.SetTag("service.available", isAvailable);
            
            _logger.LogDebug(
                "ML Classifier service availability check: {Status}",
                isAvailable ? "Available" : "Unavailable");

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML Classifier service health check failed");
            return false;
        }
    }
}
