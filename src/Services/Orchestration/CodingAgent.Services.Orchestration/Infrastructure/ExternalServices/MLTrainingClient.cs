using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for ML Classifier training endpoints.
/// Handles submission of feedback and triggering model retraining.
/// </summary>
public class MLTrainingClient : IMLTrainingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MLTrainingClient> _logger;
    private readonly ActivitySource _activitySource;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MLTrainingClient(
        HttpClient httpClient,
        ILogger<MLTrainingClient> logger,
        ActivitySource activitySource)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    public async Task SubmitFeedbackAsync(
        TrainingFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        using var activity = _activitySource.StartActivity("MLTraining.SubmitFeedback");
        activity?.SetTag("training.predicted_type", request.PredictedType);
        activity?.SetTag("training.predicted_complexity", request.PredictedComplexity);
        activity?.SetTag("training.confidence", request.Confidence);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Submitting feedback to ML Training service: Type={Type}, Complexity={Complexity}, Confidence={Confidence}",
                request.PredictedType, request.PredictedComplexity, request.Confidence);

            var response = await _httpClient.PostAsJsonAsync(
                "/train/feedback",
                request,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            stopwatch.Stop();
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Feedback submitted successfully to ML Training service in {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "ML Training service feedback submission failed after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "ML Training service feedback submission timed out after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw new HttpRequestException("ML Training service request timed out", ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Unexpected error submitting feedback to ML Training service after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<TrainingRetrainResponse> TriggerRetrainingAsync(
        TrainingRetrainRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        using var activity = _activitySource.StartActivity("MLTraining.TriggerRetraining");
        activity?.SetTag("training.min_samples", request.MinSamples);
        activity?.SetTag("training.model_version", request.ModelVersion ?? "latest");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Triggering model retraining in ML Training service: MinSamples={MinSamples}, ModelVersion={ModelVersion}",
                request.MinSamples, request.ModelVersion ?? "latest");

            var response = await _httpClient.PostAsJsonAsync(
                "/train/retrain",
                request,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TrainingRetrainResponse>(
                JsonOptions,
                cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("ML Training service returned null response");
            }

            stopwatch.Stop();
            activity?.SetTag("training.status", result.Status);
            activity?.SetTag("training.samples_used", result.SamplesUsed ?? 0);
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Model retraining triggered successfully: Status={Status}, SamplesUsed={SamplesUsed}, " +
                "NewModelVersion={NewModelVersion}, Accuracy={Accuracy}, Duration={Duration}ms",
                result.Status,
                result.SamplesUsed,
                result.NewModelVersion,
                result.Accuracy,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "ML Training service retraining trigger failed after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "ML Training service retraining trigger timed out after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw new HttpRequestException("ML Training service request timed out", ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Unexpected error triggering retraining in ML Training service after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<TrainingStatsResponse> GetTrainingStatsAsync(
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("MLTraining.GetStats");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Getting training statistics from ML Training service");

            var response = await _httpClient.GetAsync("/train/stats", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TrainingStatsResponse>(
                JsonOptions,
                cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("ML Training service returned null response");
            }

            stopwatch.Stop();
            activity?.SetTag("training.total_samples", result.TotalSamples);
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Training statistics retrieved: TotalSamples={TotalSamples}, " +
                "AverageConfidence={AverageConfidence}, Duration={Duration}ms",
                result.TotalSamples,
                result.AverageConfidence,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "ML Training service stats retrieval failed after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "ML Training service stats retrieval timed out after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw new HttpRequestException("ML Training service request timed out", ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Unexpected error getting training stats from ML Training service after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}

