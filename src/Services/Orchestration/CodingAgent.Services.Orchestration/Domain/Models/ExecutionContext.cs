namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Context information for task execution, including relevant files and metadata
/// </summary>
public class TaskExecutionContext
{
    public List<RelevantFile> RelevantFiles { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();

    public void AddValidationError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            ValidationErrors.Add(error);
        }
    }

    public void AddValidationErrors(IEnumerable<string> errors)
    {
        ValidationErrors.AddRange(errors.Where(e => !string.IsNullOrWhiteSpace(e)));
    }
}

/// <summary>
/// Represents a file relevant to task execution
/// </summary>
public class RelevantFile
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Language { get; set; }

    public RelevantFile() { }

    public RelevantFile(string path, string content, string? language = null)
    {
        Path = path;
        Content = content;
        Language = language;
    }
}
