namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Registry for managing available LLM models.
/// Detects and caches available models from Ollama and other providers.
/// </summary>
public interface IModelRegistry
{
    /// <summary>
    /// Gets all available models from all providers.
    /// </summary>
    Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available models for a specific provider.
    /// </summary>
    Task<List<ModelInfo>> GetModelsByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the model registry from all providers.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific model is available.
    /// </summary>
    Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an available model.
/// </summary>
public class ModelInfo
{
    public required string Name { get; set; }
    public required string Provider { get; set; } // "ollama", "openai", "anthropic", etc.
    public string? DisplayName { get; set; }
    public ModelCapability Capabilities { get; set; }
    public long? SizeBytes { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Model capabilities (can be combined with flags).
/// </summary>
[Flags]
public enum ModelCapability
{
    None = 0,
    CodeGeneration = 1,
    ChatCompletion = 2,
    CodeAnalysis = 4,
    CodeReview = 8,
    Documentation = 16,
    Testing = 32,
    All = CodeGeneration | ChatCompletion | CodeAnalysis | CodeReview | Documentation | Testing
}

