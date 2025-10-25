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
    /// <param name="request">The LLM request with model, messages, and parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LLM response with content, tokens, and cost</returns>
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
