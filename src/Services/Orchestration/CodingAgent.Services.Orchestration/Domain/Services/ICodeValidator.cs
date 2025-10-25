using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Interface for validating code changes.
/// </summary>
public interface ICodeValidator
{
    /// <summary>
    /// Validates the provided code changes (syntax, compilation).
    /// </summary>
    Task<ValidationResult> ValidateAsync(List<CodeChange> changes, CancellationToken cancellationToken = default);
}
