using CodingAgent.Services.Browser.Domain.Models;

namespace CodingAgent.Services.Browser.Domain.Services;

/// <summary>
/// Service for browser automation operations
/// </summary>
public interface IBrowserService
{
    /// <summary>
    /// Navigates to a URL and retrieves the page content
    /// </summary>
    /// <param name="request">Browse request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Browse result with content and metadata</returns>
    Task<BrowseResult> BrowseAsync(BrowseRequest request, CancellationToken cancellationToken = default);
}
