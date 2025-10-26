using System.Diagnostics;
using CodingAgent.Services.Browser.Domain.Configuration;
using CodingAgent.Services.Browser.Domain.Models;
using CodingAgent.Services.Browser.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace CodingAgent.Services.Browser.Infrastructure.Browser;

/// <summary>
/// Playwright-based browser service implementation
/// </summary>
public class PlaywrightBrowserService : IBrowserService
{
    private readonly IBrowserPool _browserPool;
    private readonly BrowserOptions _options;
    private readonly ILogger<PlaywrightBrowserService> _logger;
    private readonly ActivitySource _activitySource;

    public PlaywrightBrowserService(
        IBrowserPool browserPool,
        IOptions<BrowserOptions> options,
        ILogger<PlaywrightBrowserService> logger)
    {
        _browserPool = browserPool;
        _options = options.Value;
        _logger = logger;
        _activitySource = new ActivitySource("BrowserService");
    }

    /// <inheritdoc/>
    public async Task<BrowseResult> BrowseAsync(BrowseRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("BrowseUrl");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);

        var stopwatch = Stopwatch.StartNew();
        IPage? page = null;

        try
        {
            page = await _browserPool.AcquirePageAsync(request.BrowserType, cancellationToken);
            _logger.LogInformation("Navigating to {Url} with {BrowserType}", request.Url, request.BrowserType);

            var timeout = request.TimeoutMs ?? _options.Timeout;
            
            // Set default timeout
            page.SetDefaultTimeout(timeout);

            // Navigate to URL
            var waitUntil = request.WaitForNetworkIdle 
                ? WaitUntilState.NetworkIdle 
                : WaitUntilState.DOMContentLoaded;

            var response = await page.GotoAsync(request.Url, new PageGotoOptions
            {
                WaitUntil = waitUntil,
                Timeout = timeout
            });

            if (response == null)
            {
                throw new InvalidOperationException($"Failed to navigate to {request.Url}");
            }

            stopwatch.Stop();

            // Get page content and metadata
            var content = await page.ContentAsync();
            var title = await page.TitleAsync();
            var finalUrl = page.Url;
            var statusCode = response.Status;

            activity?.SetTag("status_code", statusCode);
            activity?.SetTag("load_time_ms", stopwatch.ElapsedMilliseconds);

            _logger.LogInformation("Successfully navigated to {Url} in {ElapsedMs}ms with status {StatusCode}",
                finalUrl, stopwatch.ElapsedMilliseconds, statusCode);

            return new BrowseResult
            {
                Content = content,
                Url = finalUrl,
                Title = title,
                StatusCode = statusCode,
                LoadTimeMs = stopwatch.ElapsedMilliseconds,
                BrowserType = request.BrowserType
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout navigating to {Url} after {TimeoutMs}ms", 
                request.Url, request.TimeoutMs ?? _options.Timeout);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            if (page != null)
            {
                await _browserPool.ReleasePageAsync(page);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ScreenshotResult> CaptureScreenshotAsync(ScreenshotRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CaptureScreenshot");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);
        activity?.SetTag("full_page", request.FullPage);
        activity?.SetTag("format", request.Format);

        var stopwatch = Stopwatch.StartNew();
        IPage? page = null;

        try
        {
            page = await _browserPool.AcquirePageAsync(request.BrowserType, cancellationToken);
            _logger.LogInformation("Capturing screenshot for {Url} with {BrowserType}", request.Url, request.BrowserType);

            var timeout = request.TimeoutMs ?? _options.Timeout;
            page.SetDefaultTimeout(timeout);

            // Navigate to URL
            await page.GotoAsync(request.Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = timeout
            });

            byte[] screenshotBytes;

            if (!string.IsNullOrEmpty(request.Selector))
            {
                // Element screenshot
                var element = await page.QuerySelectorAsync(request.Selector);
                if (element == null)
                {
                    throw new InvalidOperationException($"Element not found: {request.Selector}");
                }

                screenshotBytes = await element.ScreenshotAsync(new ElementHandleScreenshotOptions
                {
                    Type = request.Format.ToLowerInvariant() == "jpeg" ? ScreenshotType.Jpeg : ScreenshotType.Png,
                    Quality = request.Quality
                });
            }
            else
            {
                // Full page or viewport screenshot
                screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = request.FullPage,
                    Type = request.Format.ToLowerInvariant() == "jpeg" ? ScreenshotType.Jpeg : ScreenshotType.Png,
                    Quality = request.Quality
                });
            }

            stopwatch.Stop();

            var imageData = Convert.ToBase64String(screenshotBytes);

            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("image_size_bytes", screenshotBytes.Length);

            _logger.LogInformation("Screenshot captured for {Url} in {ElapsedMs}ms", 
                page.Url, stopwatch.ElapsedMilliseconds);

            return new ScreenshotResult
            {
                ImageData = imageData,
                Format = request.Format,
                Url = page.Url,
                BrowserType = request.BrowserType,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout capturing screenshot for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screenshot for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            if (page != null)
            {
                await _browserPool.ReleasePageAsync(page);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ExtractedContent> ExtractContentAsync(ExtractContentRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ExtractContent");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);

        var stopwatch = Stopwatch.StartNew();
        IPage? page = null;

        try
        {
            page = await _browserPool.AcquirePageAsync(request.BrowserType, cancellationToken);
            _logger.LogInformation("Extracting content from {Url} with {BrowserType}", request.Url, request.BrowserType);

            var timeout = request.TimeoutMs ?? _options.Timeout;
            page.SetDefaultTimeout(timeout);

            // Navigate to URL
            await page.GotoAsync(request.Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = timeout
            });

            string? text = null;
            List<string> links = new();
            List<string> images = new();

            // Extract text content
            if (request.ExtractText)
            {
                text = await page.InnerTextAsync("body");
            }

            // Extract links
            if (request.ExtractLinks)
            {
                var linkElements = await page.QuerySelectorAllAsync("a[href]");
                foreach (var link in linkElements)
                {
                    var href = await link.GetAttributeAsync("href");
                    if (!string.IsNullOrWhiteSpace(href))
                    {
                        links.Add(href);
                    }
                }
            }

            // Extract images
            if (request.ExtractImages)
            {
                var imageElements = await page.QuerySelectorAllAsync("img[src]");
                foreach (var img in imageElements)
                {
                    var src = await img.GetAttributeAsync("src");
                    if (!string.IsNullOrWhiteSpace(src))
                    {
                        images.Add(src);
                    }
                }
            }

            stopwatch.Stop();

            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("text_length", text?.Length ?? 0);
            activity?.SetTag("links_count", links.Count);
            activity?.SetTag("images_count", images.Count);

            _logger.LogInformation("Content extracted from {Url} in {ElapsedMs}ms: {TextLength} chars, {LinksCount} links, {ImagesCount} images",
                page.Url, stopwatch.ElapsedMilliseconds, text?.Length ?? 0, links.Count, images.Count);

            return new ExtractedContent
            {
                Text = text,
                Links = links,
                Images = images,
                Url = page.Url,
                BrowserType = request.BrowserType,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout extracting content from {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting content from {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            if (page != null)
            {
                await _browserPool.ReleasePageAsync(page);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<FormInteractionResult> InteractWithFormAsync(FormInteractionRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("InteractWithForm");
        activity?.SetTag("url", request.Url);
        activity?.SetTag("browser_type", request.BrowserType);
        activity?.SetTag("fields_count", request.Fields.Count);

        var stopwatch = Stopwatch.StartNew();
        IPage? page = null;

        try
        {
            page = await _browserPool.AcquirePageAsync(request.BrowserType, cancellationToken);
            _logger.LogInformation("Interacting with form at {Url} with {BrowserType}", request.Url, request.BrowserType);

            var timeout = request.TimeoutMs ?? _options.Timeout;
            page.SetDefaultTimeout(timeout);

            // Navigate to URL
            await page.GotoAsync(request.Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = timeout
            });

            // Fill form fields
            foreach (var field in request.Fields)
            {
                _logger.LogDebug("Filling field {Selector} with value", field.Key);
                await page.FillAsync(field.Key, field.Value);
            }

            // Submit form if button selector provided
            if (!string.IsNullOrEmpty(request.SubmitButtonSelector))
            {
                _logger.LogDebug("Clicking submit button {Selector}", request.SubmitButtonSelector);

                if (request.WaitForNavigation)
                {
                    // Click and wait for load state
                    await page.ClickAsync(request.SubmitButtonSelector);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                    {
                        Timeout = timeout
                    });
                }
                else
                {
                    await page.ClickAsync(request.SubmitButtonSelector);
                }
            }

            stopwatch.Stop();

            // Get final page state
            var content = await page.ContentAsync();
            var title = await page.TitleAsync();
            var finalUrl = page.Url;

            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("final_url", finalUrl);

            _logger.LogInformation("Form interaction completed for {Url} in {ElapsedMs}ms, final URL: {FinalUrl}",
                request.Url, stopwatch.ElapsedMilliseconds, finalUrl);

            return new FormInteractionResult
            {
                Success = true,
                Url = finalUrl,
                Title = title,
                Content = content,
                BrowserType = request.BrowserType,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout interacting with form at {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interacting with form at {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            if (page != null)
            {
                await _browserPool.ReleasePageAsync(page);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<PdfResult> GeneratePdfAsync(PdfRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GeneratePdf");
        activity?.SetTag("url", request.Url);

        var stopwatch = Stopwatch.StartNew();
        IPage? page = null;

        try
        {
            // PDF generation only works with Chromium
            page = await _browserPool.AcquirePageAsync("chromium", cancellationToken);
            _logger.LogInformation("Generating PDF for {Url}", request.Url);

            var timeout = request.TimeoutMs ?? _options.Timeout;
            page.SetDefaultTimeout(timeout);

            // Navigate to URL
            await page.GotoAsync(request.Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = timeout
            });

            // Build PDF options
            var pdfOptions = new PagePdfOptions
            {
                PrintBackground = request.PrintBackground
            };

            // Set page format or dimensions
            if (!string.IsNullOrEmpty(request.Format))
            {
                pdfOptions.Format = request.Format;
            }

            if (!string.IsNullOrEmpty(request.Width))
            {
                pdfOptions.Width = request.Width;
            }

            if (!string.IsNullOrEmpty(request.Height))
            {
                pdfOptions.Height = request.Height;
            }

            // Set margins
            if (request.MarginTop != null || request.MarginRight != null || 
                request.MarginBottom != null || request.MarginLeft != null)
            {
                pdfOptions.Margin = new Margin
                {
                    Top = request.MarginTop,
                    Right = request.MarginRight,
                    Bottom = request.MarginBottom,
                    Left = request.MarginLeft
                };
            }

            // Generate PDF
            var pdfBytes = await page.PdfAsync(pdfOptions);

            stopwatch.Stop();

            var pdfData = Convert.ToBase64String(pdfBytes);

            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("pdf_size_bytes", pdfBytes.Length);

            _logger.LogInformation("PDF generated for {Url} in {ElapsedMs}ms, size: {SizeBytes} bytes",
                page.Url, stopwatch.ElapsedMilliseconds, pdfBytes.Length);

            return new PdfResult
            {
                PdfData = pdfData,
                Url = page.Url,
                DurationMs = stopwatch.ElapsedMilliseconds,
                SizeBytes = pdfBytes.Length
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout generating PDF for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for {Url}", request.Url);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            if (page != null)
            {
                await _browserPool.ReleasePageAsync(page);
            }
        }
    }
}
