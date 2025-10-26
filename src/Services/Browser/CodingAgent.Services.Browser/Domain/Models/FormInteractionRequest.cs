namespace CodingAgent.Services.Browser.Domain.Models;

/// <summary>
/// Request model for form interaction
/// </summary>
public record FormInteractionRequest
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
    /// Form fields to fill (selector -> value)
    /// </summary>
    public Dictionary<string, string> Fields { get; init; } = new();

    /// <summary>
    /// Selector for the submit button
    /// </summary>
    public string? SubmitButtonSelector { get; init; }

    /// <summary>
    /// Whether to wait for navigation after submit
    /// </summary>
    public bool WaitForNavigation { get; init; } = true;
}
