namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Represents a response from an LLM provider.
/// </summary>
public class LlmResponse
{
    public required string Content { get; init; }
    public int TokensUsed { get; init; }
    public decimal CostUSD { get; init; }
    public string? Model { get; init; }
    public DateTime ResponseTime { get; init; } = DateTime.UtcNow;
}
