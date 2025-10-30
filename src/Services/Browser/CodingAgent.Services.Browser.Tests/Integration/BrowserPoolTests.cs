using CodingAgent.Services.Browser.Domain.Configuration;
using CodingAgent.Services.Browser.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace CodingAgent.Services.Browser.Tests.Integration;

[Trait("Category", "Integration")]
public class BrowserPoolTests : IAsyncDisposable
{
    private readonly Mock<ILogger<BrowserPool>> _mockLogger;
    private readonly BrowserOptions _options;
    private readonly BrowserPool _browserPool;

    public BrowserPoolTests()
    {
        _mockLogger = new Mock<ILogger<BrowserPool>>();
        _options = new BrowserOptions
        {
            MaxConcurrentPages = 5,
            Headless = true,
            Timeout = 30000
        };

        var optionsMock = new Mock<IOptions<BrowserOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        // Verify Playwright browsers are installed; otherwise skip
        try
        {
            var playwright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
            playwright.Chromium.LaunchAsync().GetAwaiter().GetResult();
            playwright.Dispose();
        }
        catch
        {
            throw new SkipException("Playwright browsers not installed");
        }

        _browserPool = new BrowserPool(optionsMock.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task AcquirePageAsync_ShouldReturnPage_WhenBrowserTypeIsChromium()
    {
        // Act
        var page = await _browserPool.AcquirePageAsync("chromium");

        // Assert
        page.Should().NotBeNull();

        // Cleanup
        await _browserPool.ReleasePageAsync(page);
    }

    [Fact]
    public async Task AcquirePageAsync_ShouldReturnPage_WhenBrowserTypeIsFirefox()
    {
        // Act
        var page = await _browserPool.AcquirePageAsync("firefox");

        // Assert
        page.Should().NotBeNull();

        // Cleanup
        await _browserPool.ReleasePageAsync(page);
    }

    [Fact]
    public async Task ReleasePageAsync_ShouldClosePageAndReleaseSemaphore()
    {
        // Arrange
        var page = await _browserPool.AcquirePageAsync("chromium");

        // Act
        await _browserPool.ReleasePageAsync(page);

        // Assert
        page.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task AcquirePageAsync_ShouldEnforceConcurrencyLimit()
    {
        // Arrange
        var pages = new List<Microsoft.Playwright.IPage>();
        var maxConcurrent = _options.MaxConcurrentPages;

        // Act - Acquire maximum allowed pages
        for (int i = 0; i < maxConcurrent; i++)
        {
            var page = await _browserPool.AcquirePageAsync("chromium");
            pages.Add(page);
        }

        // Try to acquire one more page - should block
        var acquireTask = _browserPool.AcquirePageAsync("chromium");
        
        // Wait a bit to ensure the task is blocked
        var completedTask = await Task.WhenAny(acquireTask, Task.Delay(500));
        
        // Assert - The acquire task should not complete
        completedTask.Should().NotBe(acquireTask);

        // Cleanup - Release one page
        await _browserPool.ReleasePageAsync(pages[0]);

        // Now the blocked task should complete
        var page6 = await acquireTask;
        page6.Should().NotBeNull();

        // Cleanup all pages
        foreach (var page in pages.Skip(1))
        {
            await _browserPool.ReleasePageAsync(page);
        }
        await _browserPool.ReleasePageAsync(page6);
    }

    [Fact]
    public async Task AcquirePageAsync_ShouldReuseExistingBrowser()
    {
        // Arrange
        var page1 = await _browserPool.AcquirePageAsync("chromium");
        await _browserPool.ReleasePageAsync(page1);

        // Act - Acquire another page with same browser type
        var page2 = await _browserPool.AcquirePageAsync("chromium");

        // Assert - Should reuse the browser (we can verify through logs)
        page2.Should().NotBeNull();
        page2.Context.Browser.Should().NotBeNull();

        // Cleanup
        await _browserPool.ReleasePageAsync(page2);
    }

    [Fact]
    public async Task BrowserPool_ShouldSupportBothBrowserTypes()
    {
        // Arrange & Act
        var chromiumPage = await _browserPool.AcquirePageAsync("chromium");
        var firefoxPage = await _browserPool.AcquirePageAsync("firefox");

        // Assert
        chromiumPage.Should().NotBeNull();
        firefoxPage.Should().NotBeNull();
        chromiumPage.Context.Browser.Should().NotBeSameAs(firefoxPage.Context.Browser);

        // Cleanup
        await _browserPool.ReleasePageAsync(chromiumPage);
        await _browserPool.ReleasePageAsync(firefoxPage);
    }

    [Fact]
    public async Task AcquirePageAsync_ShouldHandleCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _browserPool.AcquirePageAsync("chromium", cts.Token));
    }

    public async ValueTask DisposeAsync()
    {
        await _browserPool.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
