namespace CodingAgent.Services.Orchestration.Domain.Models;

/// <summary>
/// Result of code validation.
/// </summary>
public sealed class ValidationResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Success() => new() { IsSuccess = true };

    public static ValidationResult Failed(params string[] errors) => new()
    {
        IsSuccess = false,
        Errors = errors.ToList().AsReadOnly()
    };

    public static ValidationResult Failed(IEnumerable<string> errors) => new()
    {
        IsSuccess = false,
        Errors = errors.ToList().AsReadOnly()
    };
}