using System.Collections.Concurrent;

namespace CodingAgent.Services.Ollama.Domain.Services;

/// <summary>
/// In-memory implementation of token usage tracking
/// In production, this should use Redis or database for persistence
/// </summary>
public class InMemoryTokenUsageTracker : ITokenUsageTracker
{
    private readonly ConcurrentDictionary<string, TokenUsageData> _usageData = new();
    private readonly ILogger<InMemoryTokenUsageTracker> _logger;

    public InMemoryTokenUsageTracker(ILogger<InMemoryTokenUsageTracker> logger)
    {
        _logger = logger;
    }

    public Task RecordUsageAsync(int tokens, string provider)
    {
        var key = GetMonthlyKey(provider);
        
        _usageData.AddOrUpdate(
            key,
            _ => new TokenUsageData { Tokens = tokens, Month = DateTime.UtcNow.ToString("yyyy-MM") },
            (_, existing) =>
            {
                existing.Tokens += tokens;
                return existing;
            });

        _logger.LogInformation(
            "Recorded {Tokens} tokens for provider {Provider} in month {Month}",
            tokens, provider, key);

        return Task.CompletedTask;
    }

    public Task<int> GetMonthlyUsageAsync(string provider)
    {
        var key = GetMonthlyKey(provider);
        
        if (_usageData.TryGetValue(key, out var data))
        {
            _logger.LogDebug(
                "Retrieved {Tokens} tokens for provider {Provider}",
                data.Tokens, provider);
            return Task.FromResult(data.Tokens);
        }

        _logger.LogDebug("No usage data found for provider {Provider}", provider);
        return Task.FromResult(0);
    }

    public async Task<bool> IsWithinLimitAsync(int additionalTokens, string provider, int monthlyLimit)
    {
        var currentUsage = await GetMonthlyUsageAsync(provider);
        var projectedUsage = currentUsage + additionalTokens;
        var isWithinLimit = projectedUsage <= monthlyLimit;

        _logger.LogDebug(
            "Token limit check for {Provider}: Current={Current}, Additional={Additional}, " +
            "Projected={Projected}, Limit={Limit}, WithinLimit={WithinLimit}",
            provider, currentUsage, additionalTokens, projectedUsage, monthlyLimit, isWithinLimit);

        return isWithinLimit;
    }

    private static string GetMonthlyKey(string provider)
    {
        var month = DateTime.UtcNow.ToString("yyyy-MM");
        return $"{provider}:{month}";
    }

    private class TokenUsageData
    {
        public int Tokens { get; set; }
        public string Month { get; set; } = string.Empty;
    }
}
