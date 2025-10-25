using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Client interface for ML Classifier service.
/// Handles communication with the ML classification endpoint.
/// </summary>
public interface IMLClassifierClient
{
    /// <summary>
    /// Classifies a task using the ML Classifier service.
    /// </summary>
    /// <param name="request">Classification request with task details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Classification response with complexity and recommendations</returns>
    /// <exception cref="HttpRequestException">If the ML service is unavailable or returns an error</exception>
    Task<ClassificationResponse> ClassifyAsync(ClassificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the ML Classifier service is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the service is healthy, false otherwise</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
