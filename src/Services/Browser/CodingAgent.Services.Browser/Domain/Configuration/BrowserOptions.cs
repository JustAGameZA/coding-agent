namespace CodingAgent.Services.Browser.Domain.Configuration;

/// <summary>
/// Configuration options for browser service
/// </summary>
public class BrowserOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Browser";

    /// <summary>
    /// Whether to run browsers in headless mode
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Default timeout in milliseconds
    /// </summary>
    public int Timeout { get; set; } = 30000;

    /// <summary>
    /// User agent string
    /// </summary>
    public string UserAgent { get; set; } = "CodingAgent/2.0";

    /// <summary>
    /// Whether to block images
    /// </summary>
    public bool BlockImages { get; set; } = false;

    /// <summary>
    /// Whether to block CSS
    /// </summary>
    public bool BlockCSS { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent browser pages
    /// </summary>
    public int MaxConcurrentPages { get; set; } = 5;
}
