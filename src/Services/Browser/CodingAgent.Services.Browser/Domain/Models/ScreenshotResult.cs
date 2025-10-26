namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Result of a screenshot capture
/// </summary>
public record ScreenshotResult
{
    /// <summary>
    /// Base64 encoded image data
    /// </summary>
    public required string ImageData { get; init; }

    /// <summary>
    /// Image format (png or jpeg)
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// The final URL after any redirects
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Browser type used
    /// </summary>
    public required string BrowserType { get; init; }

    /// <summary>
    /// Time taken in milliseconds
    /// </summary>
    public long DurationMs { get; init; }
}
