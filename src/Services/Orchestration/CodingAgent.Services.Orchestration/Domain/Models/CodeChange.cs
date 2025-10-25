namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Represents a code change parsed from LLM response
/// </summary>
public class CodeChange
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Language { get; set; }
    public ChangeType Type { get; set; } = ChangeType.Modify;

    public CodeChange() { }

    public CodeChange(string filePath, string content, string? language = null, ChangeType type = ChangeType.Modify)
    {
        FilePath = filePath;
        Content = content;
        Language = language;
        Type = type;
    }
}

/// <summary>
/// Type of code change
/// </summary>
public enum ChangeType
{
    Create,
    Modify,
    Delete
}
