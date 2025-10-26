namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Request model for browsing a URL
/// </summary>
public record BrowseRequest
{
    /// <summary>
    /// The URL to navigate to
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Browser type to use (chromium or firefox)
    /// </summary>
    public string BrowserType { get; init; } = "chromium";

    /// <summary>
    /// Timeout in milliseconds (default: 30000)
    /// </summary>
    public int? TimeoutMs { get; init; }

    /// <summary>
    /// Whether to wait for network idle
    /// </summary>
    public bool WaitForNetworkIdle { get; init; } = true;
}
