namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Represents a code change to be applied.
/// </summary>
public class CodeChange
{
    public required string FilePath { get; init; }
    public required string Content { get; init; }
    public string? Language { get; init; }
}
