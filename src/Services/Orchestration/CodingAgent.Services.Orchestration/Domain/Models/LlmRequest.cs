namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Request for LLM generation
/// </summary>
public class LlmRequest
{
    public string Model { get; set; } = string.Empty;
    public List<LlmMessage> Messages { get; set; } = new();
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 4000;
}

/// <summary>
/// Response from LLM generation
/// </summary>
public class LlmResponse
{
    public string Content { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// LLM message with role and content
/// </summary>
public class LlmMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
