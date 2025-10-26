namespace CodingAgent.Services.CICDMonitor.Domain.ValueObjects;

/// <summary>
/// Represents the status of a build.
/// </summary>
public enum BuildStatus
{
    /// <summary>
    /// Build is queued and waiting to start.
    /// </summary>
    Queued,

    /// <summary>
    /// Build is currently in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Build completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Build failed.
    /// </summary>
    Failure,

    /// <summary>
    /// Build was cancelled.
    /// </summary>
    Cancelled
}
