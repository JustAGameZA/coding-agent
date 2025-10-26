using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using CodingAgent.Services.CICDMonitor.Domain.ValueObjects;
using CodingAgent.Services.CICDMonitor.Infrastructure.GitHub;
using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;
using System.Diagnostics;

namespace CodingAgent.Services.CICDMonitor.Application.Services;

/// <summary>
/// Background service that polls GitHub Actions for build status updates.
/// </summary>
public class BuildMonitor : BackgroundService
{
    private readonly IGitHubActionsClient _githubClient;
    private readonly IBuildRepository _buildRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<BuildMonitor> _logger;
    private readonly IConfiguration _configuration;
    private readonly ActivitySource _activitySource;

    public BuildMonitor(
        IGitHubActionsClient githubClient,
        IBuildRepository buildRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<BuildMonitor> logger,
        IConfiguration configuration,
        ActivitySource activitySource)
    {
        _githubClient = githubClient ?? throw new ArgumentNullException(nameof(githubClient));
        _buildRepository = buildRepository ?? throw new ArgumentNullException(nameof(buildRepository));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BuildMonitor background service is starting");

        // Wait a bit before starting to ensure the app is fully initialized
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollBuildsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while polling builds");
            }

            // Poll every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("BuildMonitor background service is stopping");
    }

    private async Task PollBuildsAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("PollBuilds");

        var repositories = GetMonitoredRepositories();

        if (!repositories.Any())
        {
            _logger.LogWarning("No repositories configured for monitoring");
            return;
        }

        _logger.LogInformation("Polling {Count} repositories for build status", repositories.Count());

        foreach (var (owner, repository) in repositories)
        {
            try
            {
                await PollRepositoryBuildsAsync(owner, repository, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error polling builds for {Owner}/{Repository}",
                    owner,
                    repository);
            }
        }
    }

    private async Task PollRepositoryBuildsAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("PollRepositoryBuilds");
        activity?.SetTag("repository.owner", owner);
        activity?.SetTag("repository.name", repository);

        _logger.LogInformation("Fetching builds for {Owner}/{Repository}", owner, repository);

        var workflowRuns = await _githubClient.GetRecentWorkflowRunsAsync(
            owner,
            repository,
            cancellationToken);

        foreach (var build in workflowRuns)
        {
            try
            {
                await ProcessBuildAsync(build, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing build {WorkflowRunId} for {Owner}/{Repository}",
                    build.WorkflowRunId,
                    owner,
                    repository);
            }
        }

        // Clean up old builds beyond retention limit
        await _buildRepository.DeleteOldBuildsAsync(
            owner,
            repository,
            retentionLimit: 100,
            cancellationToken);
    }

    private async Task ProcessBuildAsync(
        Domain.Entities.Build build,
        CancellationToken cancellationToken)
    {
        // Check if we already have this build
        var existingBuild = await _buildRepository.GetByWorkflowRunIdAsync(
            build.WorkflowRunId,
            cancellationToken);

        if (existingBuild != null)
        {
            // Update if status changed
            if (existingBuild.Status != build.Status)
            {
                existingBuild.Status = build.Status;
                existingBuild.Conclusion = build.Conclusion;
                existingBuild.UpdatedAt = build.UpdatedAt;
                existingBuild.CompletedAt = build.CompletedAt;

                await _buildRepository.UpdateAsync(existingBuild, cancellationToken);

                _logger.LogInformation(
                    "Updated build {WorkflowRunId} status to {Status}",
                    build.WorkflowRunId,
                    build.Status);

                // If build failed, fetch logs and publish event
                if (build.Status == BuildStatus.Failure)
                {
                    await HandleBuildFailureAsync(existingBuild, cancellationToken);
                }
            }
        }
        else
        {
            // New build - add it
            var savedBuild = await _buildRepository.AddAsync(build, cancellationToken);

            _logger.LogInformation(
                "Detected new build {WorkflowRunId} with status {Status}",
                build.WorkflowRunId,
                build.Status);

            // If build is already failed when first detected
            if (build.Status == BuildStatus.Failure)
            {
                await HandleBuildFailureAsync(savedBuild, cancellationToken);
            }
        }
    }

    private async Task HandleBuildFailureAsync(
        Domain.Entities.Build build,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("HandleBuildFailure");
        activity?.SetTag("build.id", build.Id);
        activity?.SetTag("workflow.run.id", build.WorkflowRunId);

        _logger.LogWarning(
            "Build failed: {WorkflowRunId} for {Owner}/{Repository} on branch {Branch}",
            build.WorkflowRunId,
            build.Owner,
            build.Repository,
            build.Branch);

        try
        {
            // Fetch logs and parse error messages
            var errorMessages = await _githubClient.GetWorkflowRunLogsAsync(
                build.Owner,
                build.Repository,
                build.WorkflowRunId,
                cancellationToken);

            build.ErrorMessages = errorMessages.ToList();
            await _buildRepository.UpdateAsync(build, cancellationToken);

            // Publish BuildFailedEvent
            var buildFailedEvent = new BuildFailedEvent
            {
                BuildId = build.Id,
                Owner = build.Owner,
                Repository = build.Repository,
                Branch = build.Branch,
                CommitSha = build.CommitSha,
                WorkflowRunId = build.WorkflowRunId,
                WorkflowName = build.WorkflowName,
                ErrorMessages = build.ErrorMessages,
                Conclusion = build.Conclusion ?? "failure",
                WorkflowUrl = build.WorkflowUrl,
                FailedAt = build.CompletedAt ?? DateTime.UtcNow
            };

            await _publishEndpoint.Publish(buildFailedEvent, cancellationToken);

            _logger.LogInformation(
                "Published BuildFailedEvent for build {BuildId} with {ErrorCount} error messages",
                build.Id,
                build.ErrorMessages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to handle build failure for {WorkflowRunId}",
                build.WorkflowRunId);
        }
    }

    private IEnumerable<(string Owner, string Repository)> GetMonitoredRepositories()
    {
        var repositories = _configuration
            .GetSection("BuildMonitor:MonitoredRepositories")
            .Get<List<MonitoredRepository>>() ?? new List<MonitoredRepository>();

        return repositories.Select(r => (r.Owner, r.Repository));
    }
}

/// <summary>
/// Configuration model for monitored repositories.
/// </summary>
public class MonitoredRepository
{
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
}
