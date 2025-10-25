namespace CodingAgent.Services.Orchestration.Domain.ValueObjects;

/// <summary>
/// Represents the complexity of a coding task.
/// Used for execution strategy selection.
/// </summary>
public enum TaskComplexity
{
    /// <summary>
    /// Simple tasks (&lt; 50 LOC). Uses SingleShot strategy with gpt-4o-mini.
    /// </summary>
    Simple,

    /// <summary>
    /// Medium tasks (50-200 LOC). Uses Iterative strategy with gpt-4o.
    /// </summary>
    Medium,

    /// <summary>
    /// Complex tasks (200-1000 LOC). Uses MultiAgent strategy with gpt-4o + claude-3.5-sonnet.
    /// </summary>
    Complex,

    /// <summary>
    /// Epic tasks (&gt; 1000 LOC). Uses HybridExecution strategy with ensemble of models.
    /// </summary>
    Epic
}
