using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Interface for code validation
/// </summary>
public interface ICodeValidator
{
    /// <summary>
    /// Validates code changes for syntax and basic correctness
    /// </summary>
    /// <param name="changes">The code changes to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with success status and any errors</returns>
    Task<ValidationResult> ValidateAsync(List<CodeChange> changes, CancellationToken ct = default);
}

/// <summary>
/// Result of code validation
/// </summary>
public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsSuccess = true };
    
    public static ValidationResult Failed(params string[] errors) => new() 
    { 
        IsSuccess = false, 
        Errors = errors.ToList() 
    };

    public static ValidationResult Failed(IEnumerable<string> errors) => new()
    {
        IsSuccess = false,
        Errors = errors.ToList()
    };
}
