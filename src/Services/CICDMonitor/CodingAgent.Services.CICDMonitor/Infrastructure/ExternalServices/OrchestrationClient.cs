using CodingAgent.Services.CICDMonitor.Domain.Services;
using System.Net.Http.Json;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for the Orchestration service.
/// </summary>
public class OrchestrationClient : IOrchestrationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrchestrationClient> _logger;

    public OrchestrationClient(HttpClient httpClient, ILogger<OrchestrationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateTaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating task in Orchestration service: {Title}", request.Title);

            var response = await _httpClient.PostAsJsonAsync("/tasks", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(cancellationToken);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize CreateTaskResponse");
            }

            _logger.LogInformation("Successfully created task {TaskId} in Orchestration service", result.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task in Orchestration service");
            throw;
        }
    }
}
