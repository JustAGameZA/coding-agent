namespace CodingAgent.Services.GitHub.Domain.Entities;

/// <summary>
/// Represents a branch in a GitHub repository
/// </summary>
public class Branch
{
    public string Name { get; set; } = string.Empty;
    public string Sha { get; set; } = string.Empty;
    public bool Protected { get; set; }
}
