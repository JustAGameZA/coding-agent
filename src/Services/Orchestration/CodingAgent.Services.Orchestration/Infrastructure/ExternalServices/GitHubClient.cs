using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for GitHub service.
/// Handles PR creation requests with retry and timeout policies.
/// </summary>
public class GitHubClient : IGitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubClient> _logger;
    private readonly ActivitySource _activitySource;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GitHubClient(
        HttpClient httpClient,
        ILogger<GitHubClient> logger,
        ActivitySource activitySource)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    public async Task<GitHubPullRequest> CreatePullRequestAsync(
        string owner,
        string repo,
        string title,
        string body,
        string head,
        string @base,
        bool isDraft = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException("Owner cannot be empty", nameof(owner));
        }

        if (string.IsNullOrWhiteSpace(repo))
        {
            throw new ArgumentException("Repository name cannot be empty", nameof(repo));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(head))
        {
            throw new ArgumentException("Head branch cannot be empty", nameof(head));
        }

        if (string.IsNullOrWhiteSpace(@base))
        {
            throw new ArgumentException("Base branch cannot be empty", nameof(@base));
        }

        using var activity = _activitySource.StartActivity("GitHub.CreatePullRequest");
        activity?.SetTag("github.owner", owner);
        activity?.SetTag("github.repo", repo);
        activity?.SetTag("github.head", head);
        activity?.SetTag("github.base", @base);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Creating GitHub PR: {Owner}/{Repo} - {Title} ({Head} -> {Base})",
                owner, repo, title, head, @base);

            var request = new CreatePullRequestRequest(
                owner,
                repo,
                title,
                body,
                head,
                @base,
                isDraft);

            var response = await _httpClient.PostAsJsonAsync(
                "/pull-requests",
                request,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PullRequestResponse>(
                JsonOptions,
                cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("GitHub service returned null response");
            }

            stopwatch.Stop();

            activity?.SetTag("github.pr.number", result.Number);
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "GitHub PR created: {Owner}/{Repo}#{Number} - {Url} (duration: {Duration}ms)",
                owner, repo, result.Number, result.HtmlUrl, stopwatch.ElapsedMilliseconds);

            return new GitHubPullRequest(result.Number, result.Url, result.HtmlUrl);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "GitHub service request failed after {Duration}ms: {Owner}/{Repo}",
                stopwatch.ElapsedMilliseconds, owner, repo);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "GitHub service request timed out after {Duration}ms: {Owner}/{Repo}",
                stopwatch.ElapsedMilliseconds, owner, repo);

            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw new HttpRequestException("GitHub service request timed out", ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Unexpected error calling GitHub service after {Duration}ms: {Owner}/{Repo}",
                stopwatch.ElapsedMilliseconds, owner, repo);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = _activitySource.StartActivity("GitHub.HealthCheck");

            _logger.LogDebug("Checking GitHub service availability");

            var response = await _httpClient.GetAsync("/health", cancellationToken);
            var isAvailable = response.IsSuccessStatusCode;

            activity?.SetTag("service.available", isAvailable);

            _logger.LogDebug(
                "GitHub service availability check: {Status}",
                isAvailable ? "Available" : "Unavailable");

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub service health check failed");
            return false;
        }
    }

    // Request/Response models matching GitHub service API
    private record CreatePullRequestRequest(
        string Owner,
        string Repo,
        string Title,
        string? Body,
        string Head,
        string Base,
        bool IsDraft);

    private record PullRequestResponse(
        long GitHubId,
        int Number,
        string Owner,
        string RepositoryName,
        string Title,
        string? Body,
        string Head,
        string Base,
        string State,
        bool IsMerged,
        bool IsDraft,
        string Author,
        string Url,
        string HtmlUrl,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? MergedAt,
        DateTime? ClosedAt);
}
