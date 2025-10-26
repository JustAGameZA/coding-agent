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
}
