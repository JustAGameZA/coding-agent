using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// In-memory A/B testing engine.
/// In production, this would persist to a database.
/// </summary>
public class ABTestingEngine : IABTestingEngine
{
    private readonly ILogger<ABTestingEngine> _logger;
    private readonly Dictionary<Guid, ABTest> _tests = new();
    private readonly Dictionary<Guid, List<ABTestResult>> _results = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ABTestingEngine(ILogger<ABTestingEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ABTest> CreateTestAsync(CreateABTestRequest request, CancellationToken cancellationToken = default)
    {
        var test = new ABTest
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ModelA = request.ModelA,
            ModelB = request.ModelB,
            TaskTypeFilter = request.TaskTypeFilter,
            TrafficPercent = Math.Clamp(request.TrafficPercent, 0, 100),
            Status = ABTestStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = request.DurationDays.HasValue 
                ? DateTime.UtcNow.AddDays(request.DurationDays.Value) 
                : null,
            MinSamples = request.MinSamples
        };

        _tests[test.Id] = test;
        _results[test.Id] = new List<ABTestResult>();

        _logger.LogInformation(
            "Created A/B test {TestId}: {Name} comparing {ModelA} vs {ModelB} with {Percent}% traffic",
            test.Id, test.Name, test.ModelA, test.ModelB, test.TrafficPercent);

        return Task.FromResult(test);
    }

    public Task<ABTest?> GetActiveTestAsync(string taskType, Guid? userId, CancellationToken cancellationToken = default)
    {
        var activeTest = _tests.Values
            .Where(t => t.Status == ABTestStatus.Active)
            .Where(t => t.TaskTypeFilter == null || t.TaskTypeFilter.Equals(taskType, StringComparison.OrdinalIgnoreCase))
            .Where(t => t.EndDate == null || t.EndDate > DateTime.UtcNow)
            .OrderByDescending(t => t.StartDate)
            .FirstOrDefault();

        return Task.FromResult<ABTest?>(activeTest);
    }

    public Task<string> SelectVariantAsync(ABTest test, Guid requestId, CancellationToken cancellationToken = default)
    {
        // Consistent hashing: same requestId always gets same variant
        var hash = requestId.GetHashCode();
        var isTestTraffic = Math.Abs(hash % 100) < test.TrafficPercent;

        if (!isTestTraffic)
        {
            // Not in test traffic, return model A as default
            return Task.FromResult(test.ModelA);
        }

        // Split test traffic 50/50 between A and B
        var variant = (hash % 2 == 0) ? test.ModelA : test.ModelB;

        _logger.LogDebug(
            "A/B test {TestId}: Request {RequestId} assigned to variant {Variant}",
            test.Id, requestId, variant);

        return Task.FromResult(variant);
    }

    public async Task RecordResultAsync(Guid testId, string variant, ABTestResult result, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_results.ContainsKey(testId))
            {
                _results[testId] = new List<ABTestResult>();
            }

            _results[testId].Add(result);

            _logger.LogDebug(
                "Recorded A/B test result for test {TestId}, variant {Variant}: Success={Success}, Quality={Quality}",
                testId, variant, result.Success, result.QualityScore);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ABTestResults> GetResultsAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_tests.TryGetValue(testId, out var test))
            {
                throw new InvalidOperationException($"A/B test {testId} not found");
            }

            if (!_results.TryGetValue(testId, out var allResults))
            {
                allResults = new List<ABTestResult>();
            }

            var resultsA = allResults.Where(r => r.Variant == test.ModelA).ToList();
            var resultsB = allResults.Where(r => r.Variant == test.ModelB).ToList();

            var variantA = CalculateVariantResults(test.ModelA, resultsA);
            var variantB = CalculateVariantResults(test.ModelB, resultsB);

            var (winner, confidence, isSignificant) = DetermineWinner(variantA, variantB);

            return new ABTestResults
            {
                TestId = testId,
                VariantA = variantA,
                VariantB = variantB,
                Winner = winner,
                ConfidenceLevel = confidence,
                IsSignificant = isSignificant,
                TotalSamples = allResults.Count
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task EndTestAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        if (_tests.TryGetValue(testId, out var test))
        {
            test.Status = ABTestStatus.Completed;
            test.EndDate = DateTime.UtcNow;
            
            _logger.LogInformation("Ended A/B test {TestId}", testId);
        }

        return Task.CompletedTask;
    }

    private ABTestVariantResults CalculateVariantResults(string model, List<ABTestResult> results)
    {
        if (!results.Any())
        {
            return new ABTestVariantResults
            {
                Model = model,
                SampleCount = 0,
                SuccessRate = 0,
                AverageQualityScore = 0,
                AverageDuration = TimeSpan.Zero,
                AverageCost = 0
            };
        }

        return new ABTestVariantResults
        {
            Model = model,
            SampleCount = results.Count,
            SuccessRate = results.Count(r => r.Success) / (double)results.Count,
            AverageQualityScore = results
                .Where(r => r.QualityScore.HasValue)
                .Select(r => (double)r.QualityScore!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            AverageDuration = TimeSpan.FromMilliseconds(results.Average(r => r.Duration.TotalMilliseconds)),
            AverageCost = results.Average(r => r.Cost)
        };
    }

    private (string? winner, double confidence, bool isSignificant) DetermineWinner(
        ABTestVariantResults variantA, 
        ABTestVariantResults variantB)
    {
        // Simple statistical test: require minimum samples and significant difference
        if (variantA.SampleCount < 30 || variantB.SampleCount < 30)
        {
            return (null, 0, false);
        }

        var rateDiff = Math.Abs(variantA.SuccessRate - variantB.SuccessRate);
        
        // Simple confidence calculation based on sample sizes and difference
        var pooledRate = (variantA.SuccessRate * variantA.SampleCount + variantB.SuccessRate * variantB.SampleCount) 
            / (variantA.SampleCount + variantB.SampleCount);
        
        var se = Math.Sqrt(pooledRate * (1 - pooledRate) * (1.0 / variantA.SampleCount + 1.0 / variantB.SampleCount));
        var zScore = rateDiff / se;

        // 95% confidence threshold (z > 1.96)
        var isSignificant = zScore > 1.96;
        var confidence = Math.Min(99, Math.Max(0, zScore * 10)); // Rough confidence estimate

        if (!isSignificant)
        {
            return (null, confidence, false);
        }

        var winner = variantA.SuccessRate > variantB.SuccessRate ? "A" : "B";
        return (winner, confidence, true);
    }
}

