namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Result of a browse operation
/// </summary>
public record BrowseResult
{
    /// <summary>
    /// The HTML content of the page
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The final URL after any redirects
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The page title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Time taken to load the page in milliseconds
    /// </summary>
    public long LoadTimeMs { get; init; }

    /// <summary>
    /// Browser type used
    /// </summary>
    public required string BrowserType { get; init; }
}
