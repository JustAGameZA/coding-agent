using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Infrastructure.LLM;

/// <summary>
/// Mock implementation of ILlmClient for testing and development.
/// This will be replaced with a real implementation in a future phase.
/// </summary>
public class MockLlmClient : ILlmClient
{
    private readonly ILogger<MockLlmClient> _logger;

    public MockLlmClient(ILogger<MockLlmClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MockLlmClient is being used. This should be replaced with a real implementation.");

        // Simulate a simple response
        var response = new LlmResponse
        {
            Content = @"FILE: src/generated.cs
```csharp
// This is a mock response from MockLlmClient
// TODO: Implement real LLM integration
public class GeneratedCode
{
    public string Message { get; set; } = ""Mock Implementation"";
}
```",
            TokensUsed = 150,
            CostUSD = 0.003m,
            Model = request.Model
        };

        return Task.FromResult(response);
    }
}
