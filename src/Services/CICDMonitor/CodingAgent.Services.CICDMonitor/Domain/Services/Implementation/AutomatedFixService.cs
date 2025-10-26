using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;
using System.Text.RegularExpressions;

namespace CodingAgent.Services.CICDMonitor.Domain.Services.Implementation;

/// <summary>
/// Service for generating automated fixes for build failures.
/// </summary>
public class AutomatedFixService : IAutomatedFixService
{
    private readonly IOrchestrationClient _orchestrationClient;
    private readonly IGitHubClient _githubClient;
    private readonly IFixAttemptRepository _fixAttemptRepository;
    private readonly IBuildFailureRepository _buildFailureRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<AutomatedFixService> _logger;

    // Known error patterns that should be automatically fixed (compiled for performance)
    private static readonly IReadOnlyDictionary<string, Regex> ErrorPatterns =
        new Dictionary<string, Regex>
        {
            ["compilation_error"] = new Regex(@"error (CS|BC)\d{4}:", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["test_failure"]      = new Regex(@"Test.*failed|Failed.*test", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["lint_error"]        = new Regex(@"ESLint|TSLint|lint error", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["missing_dependency"] = new Regex(@"Cannot find module|ModuleNotFoundError", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["syntax_error"]      = new Regex(@"SyntaxError|syntax error", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["null_reference"]    = new Regex(@"NullReferenceException|null reference", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["timeout"]           = new Regex(@"timeout|timed out", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };

    public AutomatedFixService(
        IOrchestrationClient orchestrationClient,
        IGitHubClient githubClient,
        IFixAttemptRepository fixAttemptRepository,
        IBuildFailureRepository buildFailureRepository,
        IEventPublisher eventPublisher,
        ILogger<AutomatedFixService> logger)
    {
        _orchestrationClient = orchestrationClient;
        _githubClient = githubClient;
        _fixAttemptRepository = fixAttemptRepository;
        _buildFailureRepository = buildFailureRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<FixAttempt?> ProcessBuildFailureAsync(BuildFailure buildFailure, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if we should attempt a fix
            if (!ShouldAttemptFix(buildFailure.ErrorMessage))
            {
                _logger.LogInformation("Skipping automated fix for build failure {BuildId} - error pattern not recognized",
                    buildFailure.Id);
                return null;
            }

            var errorPattern = ExtractErrorPattern(buildFailure.ErrorMessage);
            _logger.LogInformation("Processing build failure {BuildId} with error pattern: {ErrorPattern}",
                buildFailure.Id, errorPattern);

            // Save build failure
            await _buildFailureRepository.CreateAsync(buildFailure, cancellationToken);

            // Create task in Orchestration service
            var taskRequest = new CreateTaskRequest
            {
                Title = $"Fix build error in {buildFailure.Repository}",
                Description = CreateFixDescription(buildFailure)
            };

            var taskResponse = await _orchestrationClient.CreateTaskAsync(taskRequest, cancellationToken);

            // Create fix attempt record
            var fixAttempt = new FixAttempt
            {
                Id = Guid.NewGuid(),
                BuildFailureId = buildFailure.Id,
                TaskId = taskResponse.Id,
                Repository = buildFailure.Repository,
                ErrorMessage = buildFailure.ErrorMessage,
                ErrorPattern = errorPattern,
                Status = FixStatus.InProgress,
                AttemptedAt = DateTime.UtcNow
            };

            await _fixAttemptRepository.CreateAsync(fixAttempt, cancellationToken);

            // Publish FixAttemptedEvent
            await _eventPublisher.PublishAsync(new FixAttemptedEvent
            {
                FixAttemptId = fixAttempt.Id,
                BuildId = buildFailure.Id,
                TaskId = taskResponse.Id,
                Repository = buildFailure.Repository,
                ErrorMessage = buildFailure.ErrorMessage,
                ErrorPattern = errorPattern
            }, cancellationToken);

            _logger.LogInformation("Created fix attempt {FixAttemptId} for build failure {BuildId} with task {TaskId}",
                fixAttempt.Id, buildFailure.Id, taskResponse.Id);

            return fixAttempt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process build failure {BuildId}", buildFailure.Id);
            throw;
        }
    }

    public async Task ProcessTaskCompletionAsync(Guid taskId, bool success, CancellationToken cancellationToken = default)
    {
        try
        {
            var fixAttempt = await _fixAttemptRepository.GetByTaskIdAsync(taskId, cancellationToken);
            if (fixAttempt == null)
            {
                _logger.LogWarning("No fix attempt found for task {TaskId}", taskId);
                return;
            }

            _logger.LogInformation("Processing task completion for fix attempt {FixAttemptId}: Success={Success}",
                fixAttempt.Id, success);

            if (success)
            {
                // Task succeeded - create PR
                try
                {
                    var (owner, repo) = ParseRepository(fixAttempt.Repository);
                    var branchName = $"automated-fix/{fixAttempt.Id}";

                    var prRequest = new CreatePullRequestRequest
                    {
                        Owner = owner,
                        Repo = repo,
                        Title = $"Automated fix for build failure",
                        Body = $"This PR was automatically generated to fix a build failure.\n\nError: {SanitizeForDescription(fixAttempt.ErrorMessage, 500)}\n\nFix Attempt ID: {fixAttempt.Id}",
                        Head = branchName,
                        Base = fixAttempt.BuildFailure?.Branch ?? "main",
                        IsDraft = false
                    };

                    var prResponse = await _githubClient.CreatePullRequestAsync(prRequest, cancellationToken);

                    // Update fix attempt
                    fixAttempt.Status = FixStatus.Succeeded;
                    fixAttempt.CompletedAt = DateTime.UtcNow;
                    fixAttempt.PullRequestNumber = prResponse.Number;
                    fixAttempt.PullRequestUrl = prResponse.HtmlUrl;

                    await _fixAttemptRepository.UpdateAsync(fixAttempt, cancellationToken);

                    // Publish FixSucceededEvent
                    await _eventPublisher.PublishAsync(new FixSucceededEvent
                    {
                        FixAttemptId = fixAttempt.Id,
                        TaskId = taskId,
                        PullRequestNumber = prResponse.Number,
                        PullRequestUrl = prResponse.HtmlUrl,
                        Repository = fixAttempt.Repository,
                        ErrorPattern = fixAttempt.ErrorPattern
                    }, cancellationToken);

                    _logger.LogInformation("Fix attempt {FixAttemptId} succeeded - created PR #{PRNumber}",
                        fixAttempt.Id, prResponse.Number);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create PR for fix attempt {FixAttemptId}", fixAttempt.Id);

                    fixAttempt.Status = FixStatus.Failed;
                    fixAttempt.CompletedAt = DateTime.UtcNow;
                    // Avoid leaking sensitive info; store exception type only
                    fixAttempt.FailureReason = $"Failed to create PR: {ex.GetType().Name}";
                    await _fixAttemptRepository.UpdateAsync(fixAttempt, cancellationToken);
                }
            }
            else
            {
                // Task failed
                fixAttempt.Status = FixStatus.Failed;
                fixAttempt.CompletedAt = DateTime.UtcNow;
                fixAttempt.FailureReason = "Task execution failed";
                await _fixAttemptRepository.UpdateAsync(fixAttempt, cancellationToken);

                _logger.LogInformation("Fix attempt {FixAttemptId} failed - task execution unsuccessful",
                    fixAttempt.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process task completion for task {TaskId}", taskId);
            throw;
        }
    }

    public bool ShouldAttemptFix(string errorMessage)
    {
        return ErrorPatterns.Values.Any(regex => regex.IsMatch(errorMessage));
    }

    public string? ExtractErrorPattern(string errorMessage)
    {
        foreach (var kvp in ErrorPatterns)
        {
            if (kvp.Value.IsMatch(errorMessage))
            {
                return kvp.Key;
            }
        }

        return null;
    }

    private static string CreateFixDescription(BuildFailure buildFailure)
    {
        var sanitizedMessage = SanitizeForDescription(buildFailure.ErrorMessage, 1000);
        var logSnippet = SanitizeForDescription(buildFailure.ErrorLog, 4000);

        return $@"Fix build error in {buildFailure.Repository}

**Branch**: {buildFailure.Branch}
**Commit**: {buildFailure.CommitSha}
**Workflow**: {buildFailure.WorkflowName ?? "N/A"}
**Job**: {buildFailure.JobName ?? "N/A"}

**Error Message**:
{sanitizedMessage}

{(string.IsNullOrEmpty(logSnippet) ? string.Empty : $@"
**Build Log (truncated)**:
```
{logSnippet}
```")}

Please analyze the error and provide a fix for this build failure.";
    }

    private static string SanitizeForDescription(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        // Remove control characters, normalize newlines, and avoid Markdown code fence confusion
        var cleaned = input.Replace("\r", string.Empty);
        cleaned = Regex.Replace(cleaned, "[\\u0000-\\u001F]", string.Empty);
        cleaned = cleaned.Replace("```", "'''");
        if (cleaned.Length > maxLength)
        {
            cleaned = cleaned.Substring(0, maxLength) + "…";
        }
        return cleaned;
    }

    private static (string owner, string repo) ParseRepository(string repository)
    {
        var parts = repository.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid repository format: {repository}. Expected format: owner/repo");
        }

        return (parts[0], parts[1]);
    }
}
