using CodingAgent.Services.CICDMonitor.Domain.Entities;

namespace CodingAgent.Services.CICDMonitor.Domain.Services;

/// <summary>
/// Service for generating automated fixes for build failures.
/// </summary>
public interface IAutomatedFixService
{
    /// <summary>
    /// Processes a build failure and attempts to generate an automated fix.
    /// </summary>
    /// <param name="buildFailure">The build failure to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created fix attempt.</returns>
    Task<FixAttempt?> ProcessBuildFailureAsync(BuildFailure buildFailure, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a completed task and creates a PR if successful.
    /// </summary>
    /// <param name="taskId">The task ID that completed.</param>
    /// <param name="success">Whether the task was successful.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessTaskCompletionAsync(Guid taskId, bool success, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a build error should be automatically fixed.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>True if the error should be fixed, false otherwise.</returns>
    bool ShouldAttemptFix(string errorMessage);

    /// <summary>
    /// Extracts the error pattern from an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>The matched error pattern, or null if no pattern matches.</returns>
    string? ExtractErrorPattern(string errorMessage);
}
