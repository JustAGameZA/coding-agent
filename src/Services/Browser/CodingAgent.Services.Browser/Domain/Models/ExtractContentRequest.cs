namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Request model for extracting content from a page
/// </summary>
public record ExtractContentRequest
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
    /// Whether to extract text content
    /// </summary>
    public bool ExtractText { get; init; } = true;

    /// <summary>
    /// Whether to extract links
    /// </summary>
    public bool ExtractLinks { get; init; } = true;

    /// <summary>
    /// Whether to extract images
    /// </summary>
    public bool ExtractImages { get; init; } = true;
}
