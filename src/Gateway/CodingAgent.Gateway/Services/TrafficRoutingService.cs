namespace CodingAgent.Gateway.Services;

/// <summary>
/// Service for managing traffic routing during cutover (10% → 50% → 100%)
/// </summary>
public class TrafficRoutingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TrafficRoutingService> _logger;

    public TrafficRoutingService(IConfiguration configuration, ILogger<TrafficRoutingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Determines if traffic should be routed to new service based on feature flags and percentage
    /// </summary>
    public bool ShouldRouteToNewService(string serviceName, string? correlationId = null)
    {
        // Get feature flag for the service
        var featureFlagKey = serviceName.ToLower() switch
        {
            "chat" => "Features:UseLegacyChat",
            "orchestration" => "Features:UseLegacyOrchestration",
            _ => null
        };

        if (featureFlagKey == null)
        {
            // Services without feature flags default to new (no legacy)
            return true;
        }

        var featureFlagSection = _configuration.GetSection(featureFlagKey);
        var useLegacy = featureFlagSection.Exists() && bool.TryParse(featureFlagSection.Value, out var legacy) && legacy;
        if (useLegacy)
        {
            return false; // Route to legacy
        }

        // Get traffic percentage from config (defaults to 100% = full cutover)
        var percentageSection = _configuration.GetSection($"TrafficRouting:{serviceName}:Percentage");
        var trafficPercentage = percentageSection.Exists() && int.TryParse(percentageSection.Value, out var pct) ? pct : 100;
        
        // If 100%, route all traffic to new
        if (trafficPercentage >= 100)
        {
            return true;
        }

        // Use correlation ID or request hash to determine routing (deterministic)
        // This ensures same request always goes to same destination during cutover
        var routingSeed = correlationId?.GetHashCode() ?? Guid.NewGuid().GetHashCode();
        var shouldRoute = Math.Abs(routingSeed % 100) < trafficPercentage;

        if (!shouldRoute)
        {
            _logger.LogDebug(
                "Routing to legacy {ServiceName}. Traffic percentage: {Percentage}%, Seed: {Seed}",
                serviceName, trafficPercentage, routingSeed);
        }

        return shouldRoute;
    }

    /// <summary>
    /// Gets the current traffic routing percentage for a service
    /// </summary>
    public int GetTrafficPercentage(string serviceName)
    {
        var section = _configuration.GetSection($"TrafficRouting:{serviceName}:Percentage");
        if (section.Exists() && int.TryParse(section.Value, out var percentage))
        {
            return percentage;
        }
        return 100; // Default to 100%
    }
}

