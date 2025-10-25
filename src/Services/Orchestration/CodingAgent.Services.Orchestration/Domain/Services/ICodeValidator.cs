using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Interface for validating code changes.
/// </summary>
public interface ICodeValidator
{
    /// <summary>
    /// Validates code changes for syntax and basic correctness.
    /// </summary>
    /// <param name="changes">The code changes to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with success status and any errors</returns>
    Task<ValidationResult> ValidateAsync(List<CodeChange> changes, CancellationToken cancellationToken = default);
}
