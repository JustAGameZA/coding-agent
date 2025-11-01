namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Represents the result of a strategy execution.
/// </summary>
public class StrategyExecutionResult
{
    public bool Success { get; init; }
    public bool HasPartialSuccess { get; init; } // Indicates partial success (some changes made but errors occurred)
    public List<CodeChange> Changes { get; init; } = new();
    public int TotalTokensUsed { get; init; }
    public decimal TotalCostUSD { get; init; }
    public TimeSpan Duration { get; init; }
    public List<string> Errors { get; init; } = new();
    public int IterationsUsed { get; init; }

    public static StrategyExecutionResult CreateSuccess(
        List<CodeChange> changes,
        int tokensUsed,
        decimal cost,
        TimeSpan duration,
        int iterationsUsed = 1)
    {
        return new StrategyExecutionResult
        {
            Success = true,
            Changes = changes,
            TotalTokensUsed = tokensUsed,
            TotalCostUSD = cost,
            Duration = duration,
            IterationsUsed = iterationsUsed
        };
    }

    public static StrategyExecutionResult CreateFailure(
        string errorMessage,
        List<string> errors,
        int tokensUsed,
        decimal cost,
        TimeSpan duration,
        int iterationsUsed = 0)
    {
        // Optimize error list construction - avoid intermediate collections
        var allErrors = new List<string>(capacity: 1 + (errors?.Count ?? 0))
        {
            errorMessage
        };
        
        if (errors is { Count: > 0 })
        {
            allErrors.AddRange(errors);
        }
        
        return new StrategyExecutionResult
        {
            Success = false,
            TotalTokensUsed = tokensUsed,
            TotalCostUSD = cost,
            Duration = duration,
            Errors = allErrors,
            IterationsUsed = iterationsUsed
        };
    }
}
