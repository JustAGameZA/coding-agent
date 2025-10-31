using System.Diagnostics;

namespace CodingAgent.Gateway.Services;

/// <summary>
/// Service for handling dual-write operations during migration period
/// Writes to both legacy and new systems for validation
/// </summary>
public class DualWriteService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DualWriteService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DualWriteService(
        IConfiguration configuration,
        ILogger<DualWriteService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Checks if dual-write is enabled for a service
    /// </summary>
    public bool IsDualWriteEnabled(string serviceName)
    {
        var section = _configuration.GetSection($"DualWrite:{serviceName}:Enabled");
        return section.Exists() && bool.TryParse(section.Value, out var enabled) && enabled;
    }

    /// <summary>
    /// Executes dual-write operation (writes to both legacy and new systems)
    /// </summary>
    public async Task<DualWriteResult> ExecuteDualWriteAsync(
        string serviceName,
        HttpRequestMessage newSystemRequest,
        HttpRequestMessage? legacySystemRequest = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsDualWriteEnabled(serviceName))
        {
            return new DualWriteResult { NewSystemWritten = true };
        }

        var results = new DualWriteResult();
        var correlationId = newSystemRequest.Headers.TryGetValues("X-Correlation-Id", out var values)
            ? values.FirstOrDefault()
            : Guid.NewGuid().ToString();

        using var activity = Activity.Current?.Source.StartActivity($"DualWrite.{serviceName}");
        activity?.SetTag("service.name", serviceName);
        activity?.SetTag("correlation.id", correlationId);

        try
        {
            // Write to new system (primary)
            var newClient = _httpClientFactory.CreateClient("new-system");
            var newResponse = await newClient.SendAsync(newSystemRequest, cancellationToken);
            results.NewSystemWritten = newResponse.IsSuccessStatusCode;
            results.NewSystemStatusCode = (int)newResponse.StatusCode;

            if (!results.NewSystemWritten)
            {
                _logger.LogWarning(
                    "Dual-write: New system write failed for {ServiceName}. Status: {StatusCode}",
                    serviceName, newResponse.StatusCode);
            }

            // Write to legacy system if configured
            if (legacySystemRequest != null)
            {
                try
                {
                    var legacyUrl = _configuration[$"DualWrite:{serviceName}:LegacyUrl"];
                    if (!string.IsNullOrEmpty(legacyUrl))
                    {
                        legacySystemRequest.RequestUri = new Uri(new Uri(legacyUrl), legacySystemRequest.RequestUri?.PathAndQuery);
                        legacySystemRequest.Headers.Remove("Host");

                        var legacyClient = _httpClientFactory.CreateClient("legacy-system");
                        var legacyResponse = await legacyClient.SendAsync(legacySystemRequest, cancellationToken);
                        results.LegacySystemWritten = legacyResponse.IsSuccessStatusCode;
                        results.LegacySystemStatusCode = (int)legacyResponse.StatusCode;

                        if (!results.LegacySystemWritten)
                        {
                            _logger.LogWarning(
                                "Dual-write: Legacy system write failed for {ServiceName}. Status: {StatusCode}",
                                serviceName, legacyResponse.StatusCode);
                        }

                        // Check for drift
                        if (results.NewSystemWritten && results.LegacySystemWritten)
                        {
                            // In a real scenario, you'd compare response bodies/IDs
                            _logger.LogDebug(
                                "Dual-write: Both systems written successfully for {ServiceName}",
                                serviceName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Dual-write: Legacy system write exception for {ServiceName}",
                        serviceName);
                    results.LegacySystemError = ex.Message;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Dual-write: New system write exception for {ServiceName}",
                serviceName);
            results.NewSystemError = ex.Message;
        }

        return results;
    }
}

/// <summary>
/// Result of a dual-write operation
/// </summary>
public class DualWriteResult
{
    public bool NewSystemWritten { get; set; }
    public bool LegacySystemWritten { get; set; }
    public int? NewSystemStatusCode { get; set; }
    public int? LegacySystemStatusCode { get; set; }
    public string? NewSystemError { get; set; }
    public string? LegacySystemError { get; set; }
}

