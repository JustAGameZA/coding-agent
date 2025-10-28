using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.ValueObjects;
using Octokit;
using System.Net;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.GitHub;

/// <summary>
/// Client for interacting with GitHub Actions API.
/// </summary>
public class GitHubActionsClient : IGitHubActionsClient
{
    private readonly IGitHubClient _client;
    private readonly ILogger<GitHubActionsClient> _logger;

    // Rate limiting: allow ~1 request/second without holding async locks during the wait
    private readonly object _rateLimitLock = new();
    private DateTime _nextAllowedUtc = DateTime.MinValue;

    public GitHubActionsClient(
        IGitHubClient client,
        ILogger<GitHubActionsClient> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Build>> GetRecentWorkflowRunsAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken = default)
    {
        await ApplyRateLimitAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Fetching workflow runs for {Owner}/{Repository}",
                owner,
                repository);

            // Retry on transient network failures (connection reset, DNS, transient TLS issues)
            const int maxAttempts = 3;
            int attempt = 0;
            WorkflowRunsResponse workflowRuns = null!;

            while (attempt < maxAttempts)
            {
                attempt++;
                try
                {
                    workflowRuns = await _client.Actions.Workflows.Runs.List(
                        owner,
                        repository);
                    break;
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    _logger.LogWarning(ex, "Transient error fetching workflow runs (attempt {Attempt}/{Max}) for {Owner}/{Repository}", attempt, maxAttempts, owner, repository);
                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError(ex, "Exceeded retry attempts fetching workflow runs for {Owner}/{Repository}", owner, repository);
                        throw new InvalidOperationException($"Failed to fetch workflow runs after {maxAttempts} attempts: {ex.Message}", ex);
                    }

                    // Exponential backoff with jitter
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(new Random().Next(100, 500));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
            }

            var builds = workflowRuns.WorkflowRuns.Select(MapToBuild).ToList();

            _logger.LogInformation(
                "Found {Count} workflow runs for {Owner}/{Repository}",
                builds.Count,
                owner,
                repository);

            return builds;
        }
        catch (ApiException ex)
        {
            // Handle common auth/visibility errors gracefully in dev (or public repos without tokens)
            if (ex.StatusCode == HttpStatusCode.Unauthorized ||
                ex.StatusCode == HttpStatusCode.Forbidden ||
                ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    ex,
                    "GitHub API returned {StatusCode} for {Owner}/{Repository} when fetching workflow runs. Returning empty set.",
                    (int)ex.StatusCode,
                    owner,
                    repository);
                return Enumerable.Empty<Build>();
            }

            _logger.LogError(
                ex,
                "Failed to fetch workflow runs for {Owner}/{Repository}",
                owner,
                repository);
            throw new InvalidOperationException(
                $"Failed to fetch workflow runs: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetWorkflowRunLogsAsync(
        string owner,
        string repository,
        long workflowRunId,
        CancellationToken cancellationToken = default)
    {
        await ApplyRateLimitAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Fetching logs for workflow run {WorkflowRunId} in {Owner}/{Repository}",
                workflowRunId,
                owner,
                repository);

            // Get workflow run jobs to access logs (with transient retry)
            const int maxAttempts = 3;
            int attempt = 0;
            WorkflowJobsResponse jobs = null!;

            while (attempt < maxAttempts)
            {
                attempt++;
                try
                {
                    jobs = await _client.Actions.Workflows.Jobs.List(
                        owner,
                        repository,
                        workflowRunId);
                    break;
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    _logger.LogWarning(ex, "Transient error fetching workflow run jobs (attempt {Attempt}/{Max}) for {Owner}/{Repository} run {RunId}", attempt, maxAttempts, owner, repository, workflowRunId);
                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError(ex, "Exceeded retry attempts fetching workflow run jobs for {Owner}/{Repository} run {RunId}", owner, repository, workflowRunId);
                        throw new InvalidOperationException($"Failed to fetch workflow run jobs after {maxAttempts} attempts: {ex.Message}", ex);
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(new Random().Next(100, 500));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
            }

            var errorMessages = new List<string>();

            foreach (var job in jobs.Jobs)
            {
                if (job.Conclusion == "failure" || job.Conclusion == "cancelled")
                {
                    // Parse job steps for error messages
                    if (job.Steps != null)
                    {
                        foreach (var step in job.Steps)
                        {
                            if (step.Conclusion == "failure")
                            {
                                errorMessages.Add($"Step '{step.Name}' failed in job '{job.Name}'");
                            }
                        }
                    }
                }
            }

            if (!errorMessages.Any())
            {
                errorMessages.Add("Build failed but no specific error details found");
            }

            _logger.LogInformation(
                "Extracted {Count} error messages from workflow run {WorkflowRunId}",
                errorMessages.Count,
                workflowRunId);

            return errorMessages;
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized ||
                ex.StatusCode == HttpStatusCode.Forbidden ||
                ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    ex,
                    "GitHub API returned {StatusCode} for run {WorkflowRunId}. Returning empty log set.",
                    (int)ex.StatusCode,
                    workflowRunId);
                return Array.Empty<string>();
            }

            _logger.LogError(
                ex,
                "Failed to fetch logs for workflow run {WorkflowRunId}",
                workflowRunId);
            throw new InvalidOperationException(
                $"Failed to fetch workflow run logs: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Heuristic check for transient exceptions that are safe to retry.
    /// </summary>
    private static bool IsTransient(Exception ex)
    {
        if (ex is System.Net.Http.HttpRequestException)
        {
            return true;
        }

        if (ex is System.IO.IOException)
        {
            return true;
        }

        if (ex is TaskCanceledException)
        {
            return true;
        }

        // Inspect inner exceptions for socket / connection reset
        if (ex.InnerException != null)
        {
            return IsTransient(ex.InnerException);
        }

        return false;
    }

    /// <summary>
    /// Applies rate limiting (~1 request/sec). If the previous request was within the last second,
    /// waits only for the remaining time, without blocking other callers on a semaphore.
    /// </summary>
    private Task ApplyRateLimitAsync(CancellationToken cancellationToken)
    {
        TimeSpan delay = TimeSpan.Zero;
        lock (_rateLimitLock)
        {
            var now = DateTime.UtcNow;
            if (now < _nextAllowedUtc)
            {
                delay = _nextAllowedUtc - now;
                _nextAllowedUtc = _nextAllowedUtc + TimeSpan.FromSeconds(1);
            }
            else
            {
                _nextAllowedUtc = now + TimeSpan.FromSeconds(1);
            }
        }

        return delay > TimeSpan.Zero
            ? Task.Delay(delay, cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Maps an Octokit WorkflowRun to a Build entity.
    /// </summary>
    private Build MapToBuild(WorkflowRun workflowRun)
    {
        var status = MapStatus(workflowRun.Status, workflowRun.Conclusion);

        return new Build
        {
            Id = Guid.NewGuid(),
            WorkflowRunId = workflowRun.Id,
            Owner = workflowRun.Repository.Owner.Login,
            Repository = workflowRun.Repository.Name,
            Branch = workflowRun.HeadBranch ?? "unknown",
            CommitSha = workflowRun.HeadSha,
            WorkflowName = workflowRun.Name ?? "Unknown Workflow",
            Status = status,
            Conclusion = workflowRun.Conclusion?.StringValue,
            WorkflowUrl = workflowRun.HtmlUrl,
            CreatedAt = workflowRun.CreatedAt.UtcDateTime,
            UpdatedAt = workflowRun.UpdatedAt.UtcDateTime,
            StartedAt = workflowRun.RunStartedAt.UtcDateTime,
            /*
             * Octokit v13 does not expose WorkflowRun.CompletedAt even though the REST API includes it.
             * We deliberately set CompletedAt = null here to avoid guessing. Downstream consumers must
             * handle nulls and, if needed, derive duration using (UpdatedAt - StartedAt) or compute a
             * fallback at the edge (e.g., when emitting a terminal failure event). Any such fallback
             * should be documented alongside the code invoking it to make accuracy trade-offs explicit.
             */
            CompletedAt = null,
            ErrorMessages = new List<string>()
        };
    }

    /// <summary>
    /// Maps GitHub Actions status and conclusion to BuildStatus.
    /// </summary>
    private BuildStatus MapStatus(StringEnum<WorkflowRunStatus> status, StringEnum<WorkflowRunConclusion>? conclusion)
    {
        if (status.StringValue == "queued")
        {
            return BuildStatus.Queued;
        }

        if (status.StringValue == "in_progress")
        {
            return BuildStatus.InProgress;
        }

        if (status.StringValue == "completed")
        {
            return conclusion?.StringValue switch
            {
                "success" => BuildStatus.Success,
                "failure" => BuildStatus.Failure,
                "cancelled" => BuildStatus.Cancelled,
                _ => BuildStatus.Failure
            };
        }

        return BuildStatus.Queued;
    }
}

/// <summary>
/// Interface for GitHub Actions client.
/// </summary>
public interface IGitHubActionsClient
{
    /// <summary>
    /// Gets recent workflow runs for a repository.
    /// </summary>
    Task<IEnumerable<Build>> GetRecentWorkflowRunsAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs for a specific workflow run and extracts error messages.
    /// </summary>
    Task<IEnumerable<string>> GetWorkflowRunLogsAsync(
        string owner,
        string repository,
        long workflowRunId,
        CancellationToken cancellationToken = default);
}
