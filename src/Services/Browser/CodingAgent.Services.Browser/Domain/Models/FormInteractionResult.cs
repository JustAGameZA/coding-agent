namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Result of a form interaction
/// </summary>
public record FormInteractionResult
{
    /// <summary>
    /// Whether the form interaction was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The final URL after form submission
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The page title after submission
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// HTML content after form submission
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Browser type used
    /// </summary>
    public required string BrowserType { get; init; }

    /// <summary>
    /// Time taken in milliseconds
    /// </summary>
    public long DurationMs { get; init; }
}
