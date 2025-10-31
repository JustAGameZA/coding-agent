namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Function definition for LLM function calling
/// </summary>
public class LlmFunction
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required object Parameters { get; init; } // JSON schema object
}

