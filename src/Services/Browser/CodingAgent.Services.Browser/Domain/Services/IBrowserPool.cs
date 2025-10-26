using Microsoft.Playwright;

namespace CodingAgent.Services.Browser.Domain.Services;

/// <summary>
/// Manages a pool of browser pages to limit concurrency
/// </summary>
public interface IBrowserPool
{
    /// <summary>
    /// Acquires a browser page from the pool
    /// </summary>
    /// <param name="browserType">Type of browser (chromium or firefox)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A page instance</returns>
    Task<IPage> AcquirePageAsync(string browserType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a browser page back to the pool
    /// </summary>
    /// <param name="page">The page to release</param>
    Task ReleasePageAsync(IPage page);
}
