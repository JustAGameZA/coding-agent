using CodingAgent.Services.CICDMonitor.Domain.Entities;

namespace CodingAgent.Services.CICDMonitor.Domain.Repositories;

/// <summary>
/// Repository for managing fix attempts.
/// </summary>
public interface IFixAttemptRepository
{
    /// <summary>
    /// Gets a fix attempt by ID.
    /// </summary>
    Task<FixAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a fix attempt by task ID.
    /// </summary>
    Task<FixAttempt?> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new fix attempt record.
    /// </summary>
    Task<FixAttempt> CreateAsync(FixAttempt fixAttempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing fix attempt.
    /// </summary>
    Task UpdateAsync(FixAttempt fixAttempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fix success statistics.
    /// </summary>
    Task<FixStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fix success rate by error pattern.
    /// </summary>
    Task<Dictionary<string, FixStatistics>> GetStatisticsByErrorPatternAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for fix attempts.
/// </summary>
public record FixStatistics
{
    /// <summary>
    /// Total number of fix attempts.
    /// </summary>
    public int TotalAttempts { get; init; }

    /// <summary>
    /// Number of successful fixes.
    /// </summary>
    public int Succeeded { get; init; }

    /// <summary>
    /// Number of failed fixes.
    /// </summary>
    public int Failed { get; init; }

    /// <summary>
    /// Number of fixes still in progress.
    /// </summary>
    public int InProgress { get; init; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate => TotalAttempts > 0 ? (Succeeded * 100.0 / TotalAttempts) : 0;
}
