namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Request model for capturing a screenshot
/// </summary>
public record ScreenshotRequest
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
    /// CSS selector for element screenshot (null for full page)
    /// </summary>
    public string? Selector { get; init; }

    /// <summary>
    /// Whether to capture full page screenshot (scrolling)
    /// </summary>
    public bool FullPage { get; init; } = false;

    /// <summary>
    /// Image format (png or jpeg)
    /// </summary>
    public string Format { get; init; } = "png";

    /// <summary>
    /// Quality (0-100) for JPEG format
    /// </summary>
    public int? Quality { get; init; }
}
