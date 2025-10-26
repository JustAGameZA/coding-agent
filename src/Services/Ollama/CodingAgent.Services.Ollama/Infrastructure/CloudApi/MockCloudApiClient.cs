using Microsoft.Extensions.Options;

namespace CodingAgent.Services.Ollama.Infrastructure.CloudApi;

/// <summary>
/// Mock implementation of cloud API client (always returns not configured)
/// Can be replaced with real implementation (OpenAI, Anthropic, etc.) when needed
/// </summary>
public class MockCloudApiClient : ICloudApiClient
{
    private readonly CloudApiOptions _options;
    private readonly ILogger<MockCloudApiClient> _logger;

    public MockCloudApiClient(
        IOptions<CloudApiOptions> options,
        ILogger<MockCloudApiClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if cloud API is configured
    /// </summary>
    public bool IsConfigured()
    {
        var configured = !string.IsNullOrWhiteSpace(_options.ApiKey) && 
                        !string.IsNullOrWhiteSpace(_options.Provider) &&
                        !_options.Provider.Equals("none", StringComparison.OrdinalIgnoreCase);
        
        _logger.LogDebug("Cloud API configured: {IsConfigured}", configured);
        return configured;
    }

    /// <summary>
    /// Checks if tokens are available within monthly limit
    /// </summary>
    public Task<bool> HasTokensAvailableAsync()
    {
        if (!IsConfigured())
        {
            _logger.LogDebug("Cloud API not configured, no tokens available");
            return Task.FromResult(false);
        }

        // In real implementation, this would check usage tracking
        _logger.LogDebug("Cloud API tokens available (mock implementation)");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Generates text using cloud API (mock implementation throws)
    /// </summary>
    public Task<string> GenerateAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MockCloudApiClient.GenerateAsync called but not implemented");
        throw new NotImplementedException(
            "MockCloudApiClient does not implement actual generation. " +
            "Use a real cloud API implementation (OpenAI, Anthropic, etc.)");
    }
}

/// <summary>
/// Configuration options for cloud API fallback
/// </summary>
public class CloudApiOptions
{
    public const string SectionName = "CloudApi";

    /// <summary>
    /// Cloud API provider (e.g., "openai", "anthropic", "none")
    /// </summary>
    public string Provider { get; set; } = "none";

    /// <summary>
    /// API key for the cloud provider
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API endpoint URL (optional, uses provider default if not set)
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Monthly token limit
    /// </summary>
    public int MonthlyTokenLimit { get; set; } = 100_000;
}
