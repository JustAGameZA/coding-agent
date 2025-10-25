using CodingAgent.SharedKernel.Domain.Entities;

namespace CodingAgent.Services.Orchestration.Domain.Entities;

/// <summary>
/// Represents the result of a task execution with detailed changes and metrics
/// </summary>
public class ExecutionResult : BaseEntity
{
    public Guid ExecutionId { get; private set; }
    public bool Success { get; private set; }
    public string? Changes { get; private set; }
    public int TokensUsed { get; private set; }
    public decimal CostUSD { get; private set; }
    public string? ErrorDetails { get; private set; }
    public int FilesChanged { get; private set; }
    public int LinesAdded { get; private set; }
    public int LinesRemoved { get; private set; }

    // Navigation property
    public TaskExecution? Execution { get; private set; }

    // EF Core constructor
    private ExecutionResult() { }

    public ExecutionResult(Guid executionId, bool success, int tokensUsed, decimal costUSD)
    {
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("Execution ID cannot be empty", nameof(executionId));
        }

        if (tokensUsed < 0)
        {
            throw new ArgumentException("Tokens used cannot be negative", nameof(tokensUsed));
        }

        if (costUSD < 0)
        {
            throw new ArgumentException("Cost cannot be negative", nameof(costUSD));
        }

        ExecutionId = executionId;
        Success = success;
        TokensUsed = tokensUsed;
        CostUSD = costUSD;
    }

    public void SetChanges(string changes, int filesChanged, int linesAdded, int linesRemoved)
    {
        if (string.IsNullOrWhiteSpace(changes))
        {
            throw new ArgumentException("Changes cannot be empty", nameof(changes));
        }

        if (filesChanged < 0)
        {
            throw new ArgumentException("Files changed cannot be negative", nameof(filesChanged));
        }

        if (linesAdded < 0)
        {
            throw new ArgumentException("Lines added cannot be negative", nameof(linesAdded));
        }

        if (linesRemoved < 0)
        {
            throw new ArgumentException("Lines removed cannot be negative", nameof(linesRemoved));
        }

        Changes = changes;
        FilesChanged = filesChanged;
        LinesAdded = linesAdded;
        LinesRemoved = linesRemoved;
        MarkAsUpdated();
    }

    public void SetError(string errorDetails)
    {
        if (string.IsNullOrWhiteSpace(errorDetails))
        {
            throw new ArgumentException("Error details cannot be empty", nameof(errorDetails));
        }

        ErrorDetails = errorDetails;
        Success = false;
        MarkAsUpdated();
    }
}
