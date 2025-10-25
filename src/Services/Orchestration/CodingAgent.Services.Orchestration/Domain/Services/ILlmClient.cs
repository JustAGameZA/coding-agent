using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Interface for interacting with LLM providers.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Generates a response from the LLM based on the provided request.
    /// </summary>
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
