using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Planner agent responsible for breaking down complex tasks into subtasks.
/// </summary>
public interface IPlannerAgent
{
    /// <summary>
    /// Creates a plan by breaking down a complex task into manageable subtasks.
    /// </summary>
    /// <param name="task">The coding task to plan</param>
    /// <param name="context">Execution context with relevant files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task plan with subtasks</returns>
    Task<AgentResult> CreatePlanAsync(
        CodingTask task, 
        TaskExecutionContext context, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Coder agent responsible for implementing code changes for subtasks.
/// </summary>
public interface ICoderAgent
{
    /// <summary>
    /// Implements code changes for a specific subtask.
    /// </summary>
    /// <param name="subTask">The subtask to implement</param>
    /// <param name="context">Execution context with relevant files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with code changes</returns>
    Task<AgentResult> ImplementSubTaskAsync(
        SubTask subTask,
        TaskExecutionContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reviewer agent responsible for reviewing generated code changes.
/// </summary>
public interface IReviewerAgent
{
    /// <summary>
    /// Reviews code changes from multiple agents.
    /// </summary>
    /// <param name="changes">The code changes to review</param>
    /// <param name="originalTask">The original task for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with review feedback</returns>
    Task<AgentResult> ReviewChangesAsync(
        List<CodeChange> changes,
        CodingTask originalTask,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Tester agent responsible for generating test cases.
/// </summary>
public interface ITesterAgent
{
    /// <summary>
    /// Generates test cases for the implemented code changes.
    /// </summary>
    /// <param name="changes">The code changes to generate tests for</param>
    /// <param name="originalTask">The original task for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with test code</returns>
    Task<AgentResult> GenerateTestsAsync(
        List<CodeChange> changes,
        CodingTask originalTask,
        CancellationToken cancellationToken = default);
}
