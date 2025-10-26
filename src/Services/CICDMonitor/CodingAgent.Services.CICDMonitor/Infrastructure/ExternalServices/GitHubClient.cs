using CodingAgent.Services.CICDMonitor.Domain.Services;
using System.Net.Http.Json;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for the GitHub service.
/// </summary>
public class GitHubClient : IGitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubClient> _logger;

    public GitHubClient(HttpClient httpClient, ILogger<GitHubClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreatePullRequestResponse> CreatePullRequestAsync(CreatePullRequestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating pull request in GitHub service: {Owner}/{Repo} {Head} -> {Base}",
                request.Owner, request.Repo, request.Head, request.Base);

            var response = await _httpClient.PostAsJsonAsync("/pull-requests", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CreatePullRequestResponse>(cancellationToken);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize CreatePullRequestResponse");
            }

            _logger.LogInformation("Successfully created PR #{Number} in {Owner}/{Repo}",
                result.Number, result.Owner, result.RepositoryName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pull request in GitHub service");
            throw;
        }
    }
}
