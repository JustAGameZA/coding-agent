namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Extracted content from a page
/// </summary>
public record ExtractedContent
{
    /// <summary>
    /// The text content of the page
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// List of links (href attributes)
    /// </summary>
    public List<string> Links { get; init; } = new();

    /// <summary>
    /// List of image URLs (src attributes)
    /// </summary>
    public List<string> Images { get; init; } = new();

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
