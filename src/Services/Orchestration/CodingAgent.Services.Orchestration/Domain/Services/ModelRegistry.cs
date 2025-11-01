using CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Registry for managing available LLM models.
/// Detects and caches available models from Ollama and other providers.
/// </summary>
public class ModelRegistry : IModelRegistry
{
    private readonly OllamaServiceClient? _ollamaServiceClient;
    private readonly ILogger<ModelRegistry> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    
    private List<ModelInfo> _cachedModels = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public ModelRegistry(
        ILogger<ModelRegistry> logger,
        OllamaServiceClient? ollamaServiceClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ollamaServiceClient = ollamaServiceClient;
    }

    public async Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        // Refresh cache if expired
        if (DateTime.UtcNow - _lastRefresh > _cacheExpiration)
        {
            await RefreshAsync(cancellationToken);
        }

        return _cachedModels.Where(m => m.IsAvailable).ToList();
    }

    public async Task<List<ModelInfo>> GetModelsByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var allModels = await GetAvailableModelsAsync(cancellationToken);
        return allModels.Where(m => m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Refreshing model registry from all providers");

            var models = new List<ModelInfo>();

            // Get models from Ollama
            if (_ollamaServiceClient != null)
            {
                try
                {
                    var ollamaModels = await _ollamaServiceClient.ListModelsAsync(cancellationToken);
                    foreach (var ollamaModel in ollamaModels)
                    {
                        models.Add(new ModelInfo
                        {
                            Name = ollamaModel.Name,
                            Provider = "ollama",
                            DisplayName = ollamaModel.Name,
                            Capabilities = DetermineCapabilities(ollamaModel.Name),
                            SizeBytes = ollamaModel.Size,
                            IsAvailable = true,
                            LastUpdated = DateTime.UtcNow
                        });
                    }

                    _logger.LogInformation("Discovered {Count} models from Ollama", ollamaModels.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to query Ollama for models");
                }
            }

            // Add default cloud models (always available if configured)
            models.AddRange(GetDefaultCloudModels());

            _cachedModels = models;
            _lastRefresh = DateTime.UtcNow;

            _logger.LogInformation("Model registry refreshed: {Count} total models available", models.Count);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<bool> IsModelAvailableAsync(string modelName, CancellationToken cancellationToken = default)
    {
        var models = await GetAvailableModelsAsync(cancellationToken);
        return models.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
    }

    private List<ModelInfo> GetDefaultCloudModels()
    {
        // Default models that might be available via cloud APIs
        return new List<ModelInfo>
        {
            new() { Name = "gpt-4o", Provider = "openai", DisplayName = "GPT-4o", Capabilities = ModelCapability.All, IsAvailable = true, LastUpdated = DateTime.UtcNow },
            new() { Name = "gpt-4o-mini", Provider = "openai", DisplayName = "GPT-4o Mini", Capabilities = ModelCapability.All, IsAvailable = true, LastUpdated = DateTime.UtcNow },
            new() { Name = "gpt-4", Provider = "openai", DisplayName = "GPT-4", Capabilities = ModelCapability.All, IsAvailable = true, LastUpdated = DateTime.UtcNow },
            new() { Name = "claude-3.5-sonnet", Provider = "anthropic", DisplayName = "Claude 3.5 Sonnet", Capabilities = ModelCapability.All, IsAvailable = true, LastUpdated = DateTime.UtcNow }
        };
    }

    private ModelCapability DetermineCapabilities(string modelName)
    {
        var lowerName = modelName.ToLowerInvariant();
        
        // Code-focused models
        if (lowerName.Contains("code") || lowerName.Contains("coder"))
        {
            return ModelCapability.CodeGeneration | ModelCapability.CodeAnalysis | ModelCapability.CodeReview;
        }

        // General-purpose models
        return ModelCapability.All;
    }
}

