using CodingAgent.Services.Browser.Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace CodingAgent.Services.Browser.Domain.Services;

/// <summary>
/// Manages a pool of browser pages with concurrency limiting
/// </summary>
public class BrowserPool : IBrowserPool, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly BrowserOptions _options;
    private readonly ILogger<BrowserPool> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _chromiumBrowser;
    private IBrowser? _firefoxBrowser;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    public BrowserPool(
        IOptions<BrowserOptions> options,
        ILogger<BrowserPool> logger)
    {
        _options = options.Value;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentPages, _options.MaxConcurrentPages);
    }

    /// <inheritdoc/>
    public async Task<IPage> AcquirePageAsync(string browserType, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            await EnsurePlaywrightInitializedAsync();

            var browser = browserType.ToLowerInvariant() switch
            {
                "firefox" => await GetOrCreateFirefoxBrowserAsync(),
                "chromium" or _ => await GetOrCreateChromiumBrowserAsync()
            };

            var page = await browser.NewPageAsync();
            
            // Set user agent if configured
            if (!string.IsNullOrEmpty(_options.UserAgent))
            {
                await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
                {
                    ["User-Agent"] = _options.UserAgent
                });
            }

            _logger.LogDebug("Acquired page from {BrowserType} browser pool. Available slots: {Available}/{Max}",
                browserType, _semaphore.CurrentCount, _options.MaxConcurrentPages);

            return page;
        }
        catch
        {
            // If page creation fails, release the semaphore
            _semaphore.Release();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ReleasePageAsync(IPage page)
    {
        try
        {
            await page.CloseAsync();
            _logger.LogDebug("Released page back to pool. Available slots: {Available}/{Max}",
                _semaphore.CurrentCount + 1, _options.MaxConcurrentPages);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing page, will still release semaphore");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsurePlaywrightInitializedAsync()
    {
        if (_playwright != null)
        {
            return;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_playwright == null)
            {
                _playwright = await Playwright.CreateAsync();
                _logger.LogInformation("Playwright initialized successfully");
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task<IBrowser> GetOrCreateChromiumBrowserAsync()
    {
        if (_chromiumBrowser != null)
        {
            return _chromiumBrowser;
        }

        _chromiumBrowser = await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless
        });

        _logger.LogInformation("Chromium browser launched in {Mode} mode", 
            _options.Headless ? "headless" : "headed");

        return _chromiumBrowser;
    }

    private async Task<IBrowser> GetOrCreateFirefoxBrowserAsync()
    {
        if (_firefoxBrowser != null)
        {
            return _firefoxBrowser;
        }

        _firefoxBrowser = await _playwright!.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless
        });

        _logger.LogInformation("Firefox browser launched in {Mode} mode",
            _options.Headless ? "headless" : "headed");

        return _firefoxBrowser;
    }

    public async ValueTask DisposeAsync()
    {
        if (_chromiumBrowser != null)
        {
            await _chromiumBrowser.CloseAsync();
            await _chromiumBrowser.DisposeAsync();
        }

        if (_firefoxBrowser != null)
        {
            await _firefoxBrowser.CloseAsync();
            await _firefoxBrowser.DisposeAsync();
        }

        _playwright?.Dispose();
        _semaphore.Dispose();
        _initializationLock.Dispose();

        _logger.LogInformation("Browser pool disposed");
    }
}
