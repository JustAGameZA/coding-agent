namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Context information for code execution.
/// </summary>
public class TaskExecutionContext
{
    public List<RelevantFile> RelevantFiles { get; init; } = new();
    public List<string> ValidationErrors { get; init; } = new();
    public int Iteration { get; set; } = 0;
}

/// <summary>
/// Represents a relevant file with its content.
/// </summary>
public class RelevantFile
{
    public required string Path { get; init; }
    public required string Content { get; init; }
    public string? Language { get; init; }
}
