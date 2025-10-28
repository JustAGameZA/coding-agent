namespace CodingAgent.Services.Orchestration.Domain.ValueObjects;

/// <summary>
/// Configuration for the target GitHub repository used when creating pull requests.
/// </summary>
public sealed class GitHubRepositoryOptions
{
    /// <summary>Repository owner (organization or user).</summary>
    public string? Owner { get; init; }

    /// <summary>Repository name.</summary>
    public string? Name { get; init; }

    /// <summary>Base branch to target for PRs (e.g., main).</summary>
    public string? BaseBranch { get; init; } = "main";

    /// <summary>Prefix for head branches created for tasks (default: "task/").</summary>
    public string? HeadPrefix { get; init; } = "task/";

    /// <summary>Default author label to set on events until JWT auth is wired.</summary>
    public string? DefaultAuthor { get; init; }
}
