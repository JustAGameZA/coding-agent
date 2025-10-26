namespace CodingAgent.Services.Ollama.Domain.ValueObjects;

/// <summary>
/// Request to generate text using Ollama
/// </summary>
public record OllamaGenerateRequest
{
    /// <summary>
    /// Model name to use for generation
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The prompt to generate from
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// System message (optional)
    /// </summary>
    public string? System { get; init; }

    /// <summary>
    /// Temperature (0.0 - 1.0)
    /// </summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; init; } = false;

    /// <summary>
    /// Additional context (optional)
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// Response from Ollama generation
/// </summary>
public record OllamaGenerateResponse
{
    /// <summary>
    /// The model used for generation
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The generated response text
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    public int PromptEvalCount { get; init; }

    /// <summary>
    /// Number of tokens in the response
    /// </summary>
    public int EvalCount { get; init; }

    /// <summary>
    /// Total tokens used
    /// </summary>
    public int TotalTokens => PromptEvalCount + EvalCount;

    /// <summary>
    /// Time taken for evaluation in nanoseconds
    /// </summary>
    public long EvalDuration { get; init; }

    /// <summary>
    /// Time taken for evaluation in milliseconds
    /// </summary>
    public double EvalDurationMs => EvalDuration / 1_000_000.0;

    /// <summary>
    /// Whether the generation was completed successfully
    /// </summary>
    public bool Done { get; init; } = true;

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
