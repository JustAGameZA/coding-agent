using CodingAgent.Services.Orchestration.Domain.Models;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Interface for LLM (Large Language Model) client communication
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Generates a response from the LLM based on the request
    /// </summary>
    /// <param name="request">The LLM request with model, messages, and parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>LLM response with content, tokens, and cost</returns>
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct = default);
}
