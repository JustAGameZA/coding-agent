namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Engine for conducting A/B tests on different models.
/// </summary>
public interface IABTestingEngine
{
    /// <summary>
    /// Creates a new A/B test.
    /// </summary>
    Task<ABTest> CreateTestAsync(CreateABTestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active A/B test for a specific request context.
    /// </summary>
    Task<ABTest?> GetActiveTestAsync(string taskType, Guid? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines which variant (model) to use for a given request.
    /// </summary>
    Task<string> SelectVariantAsync(ABTest test, Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a test result.
    /// </summary>
    Task RecordResultAsync(Guid testId, string variant, ABTestResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets test results and determines winner.
    /// </summary>
    Task<ABTestResults> GetResultsAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends an A/B test.
    /// </summary>
    Task EndTestAsync(Guid testId, CancellationToken cancellationToken = default);
}

/// <summary>
/// An A/B test comparing two models.
/// </summary>
public class ABTest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelA { get; set; } = string.Empty;
    public string ModelB { get; set; } = string.Empty;
    public string? TaskTypeFilter { get; set; } // null = all task types
    public int TrafficPercent { get; set; } = 10; // Percentage of traffic to test (0-100)
    public ABTestStatus Status { get; set; } = ABTestStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public int MinSamples { get; set; } = 100; // Minimum samples before considering test complete
}

public enum ABTestStatus
{
    Active,
    Completed,
    Cancelled
}

/// <summary>
/// Request to create an A/B test.
/// </summary>
public class CreateABTestRequest
{
    public required string Name { get; set; }
    public required string ModelA { get; set; }
    public required string ModelB { get; set; }
    public string? TaskTypeFilter { get; set; }
    public int TrafficPercent { get; set; } = 10;
    public int? DurationDays { get; set; }
    public int MinSamples { get; set; } = 100;
}

/// <summary>
/// Result of a model execution in an A/B test.
/// </summary>
public class ABTestResult
{
    public required Guid RequestId { get; set; }
    public required string Variant { get; set; }
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public int? QualityScore { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Aggregated results of an A/B test.
/// </summary>
public class ABTestResults
{
    public required Guid TestId { get; set; }
    public required ABTestVariantResults VariantA { get; set; }
    public required ABTestVariantResults VariantB { get; set; }
    public string? Winner { get; set; } // "A", "B", or null if not significant
    public double ConfidenceLevel { get; set; }
    public bool IsSignificant { get; set; }
    public int TotalSamples { get; set; }
}

/// <summary>
/// Results for a single variant in an A/B test.
/// </summary>
public class ABTestVariantResults
{
    public required string Model { get; set; }
    public int SampleCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageQualityScore { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public decimal AverageCost { get; set; }
}

