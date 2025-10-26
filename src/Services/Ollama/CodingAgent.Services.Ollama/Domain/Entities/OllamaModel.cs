namespace CodingAgent.Services.Ollama.Domain.Entities;

/// <summary>
/// Represents an Ollama model available in the system
/// </summary>
public class OllamaModel
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Model name as used in Ollama (e.g., "codellama:13b", "deepseek-coder:6.7b")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "CodeLlama 13B")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Model family (e.g., "codellama", "deepseek", "mistral")
    /// </summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>
    /// Model size in GB
    /// </summary>
    public double SizeGB { get; set; }

    /// <summary>
    /// Parameter count (e.g., "13B", "7B", "Unknown")
    /// </summary>
    public string ParameterCount { get; set; } = "Unknown";

    /// <summary>
    /// Whether the model is currently available in the Ollama backend
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// When the model was last synced from Ollama backend
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the model was first discovered
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Model description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Minimum VRAM required in GB (estimated)
    /// </summary>
    public double MinVramGB { get; set; }

    /// <summary>
    /// Model tags from Ollama (e.g., "code", "chat", "instruct")
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{DisplayName} ({Name}) - {SizeGB:F1}GB, {ParameterCount} params";
    }
}
