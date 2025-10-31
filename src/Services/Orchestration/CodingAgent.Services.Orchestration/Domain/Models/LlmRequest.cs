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
    public List<LlmFunction>? Functions { get; init; } // For function calling
    public string? FunctionCall { get; init; } // "auto", "none", or function name
}

/// <summary>
/// Response from LLM generation.
/// </summary>
public class LlmResponse
{
    public required string Content { get; init; }
    public int TokensUsed { get; init; }
    public decimal Cost { get; init; }
    public string? Model { get; init; }
}

/// <summary>
/// Represents a message in the conversation with the LLM.
/// </summary>
public class LlmMessage
{
    public required string Role { get; init; } // "system", "user", "assistant"
    public required string Content { get; init; }
}
