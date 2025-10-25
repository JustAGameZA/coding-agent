namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Result of code validation.
/// </summary>
public class ValidationResult
{
    public bool IsSuccess { get; init; }
    public List<string> Errors { get; init; } = new();

    public static ValidationResult Success() => new() { IsSuccess = true };
    
    public static ValidationResult Failed(params string[] errors) => new() 
    { 
        IsSuccess = false, 
        Errors = errors.ToList() 
    };
}
