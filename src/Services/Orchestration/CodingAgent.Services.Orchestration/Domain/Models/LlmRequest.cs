namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Represents a request to an LLM provider.
/// </summary>
public class LlmRequest
{
    public required string Model { get; init; }
    public required List<LlmMessage> Messages { get; init; }
    public double Temperature { get; init; } = 0.3;
    public int MaxTokens { get; init; } = 4000;
}

/// <summary>
/// Represents a message in the conversation with the LLM.
/// </summary>
public class LlmMessage
{
    public required string Role { get; init; } // "system", "user", "assistant"
    public required string Content { get; init; }
}
