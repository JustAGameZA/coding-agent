namespace CodingAgent.Services.Ollama.Domain.Services;

/// <summary>
/// Service for tracking token usage and enforcing monthly limits
/// </summary>
public interface ITokenUsageTracker
{
    /// <summary>
    /// Records token usage for the current month
    /// </summary>
    /// <param name="tokens">Number of tokens used</param>
    /// <param name="provider">Cloud API provider name</param>
    Task RecordUsageAsync(int tokens, string provider);

    /// <summary>
    /// Gets the current month's token usage
    /// </summary>
    /// <param name="provider">Cloud API provider name</param>
    /// <returns>Total tokens used this month</returns>
    Task<int> GetMonthlyUsageAsync(string provider);

    /// <summary>
    /// Checks if monthly limit would be exceeded with additional tokens
    /// </summary>
    /// <param name="additionalTokens">Number of tokens to check</param>
    /// <param name="provider">Cloud API provider name</param>
    /// <param name="monthlyLimit">Monthly limit to enforce</param>
    /// <returns>True if usage would be within limit</returns>
    Task<bool> IsWithinLimitAsync(int additionalTokens, string provider, int monthlyLimit);
}
