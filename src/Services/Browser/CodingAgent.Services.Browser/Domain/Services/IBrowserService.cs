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

    /// <summary>
    /// Captures a screenshot of a page or element
    /// </summary>
    /// <param name="request">Screenshot request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Screenshot result with base64 encoded image</returns>
    Task<ScreenshotResult> CaptureScreenshotAsync(ScreenshotRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts content (text, links, images) from a page
    /// </summary>
    /// <param name="request">Extract content request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted content with text, links, and images</returns>
    Task<ExtractedContent> ExtractContentAsync(ExtractContentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Interacts with a form (fill fields and submit)
    /// </summary>
    /// <param name="request">Form interaction request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Form interaction result with final page state</returns>
    Task<FormInteractionResult> InteractWithFormAsync(FormInteractionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF from a page
    /// </summary>
    /// <param name="request">PDF request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF result with base64 encoded PDF data</returns>
    Task<PdfResult> GeneratePdfAsync(PdfRequest request, CancellationToken cancellationToken = default);
}
