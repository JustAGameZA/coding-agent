using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Infrastructure.LLM;

/// <summary>
/// Mock implementation of ICodeValidator for testing and development.
/// This will be replaced with a real implementation in a future phase.
/// </summary>
public class MockCodeValidator : ICodeValidator
{
    private readonly ILogger<MockCodeValidator> _logger;

    public MockCodeValidator(ILogger<MockCodeValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ValidationResult> ValidateAsync(List<CodeChange> changes, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MockCodeValidator is being used. This should be replaced with a real implementation.");

        if (changes == null || changes.Count == 0)
        {
            return Task.FromResult(ValidationResult.Failed("No code changes provided"));
        }

        // Simple mock validation - always succeeds for now
        _logger.LogInformation("Mock validation passed for {ChangeCount} code change(s)", changes.Count);
        
        return Task.FromResult(ValidationResult.Success());
    }
}
