using CodingAgent.Services.Browser.Domain.Configuration;
using CodingAgent.Services.Browser.Domain.Models;
using CodingAgent.Services.Browser.Domain.Services;
using CodingAgent.Services.Browser.Infrastructure.Browser;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Moq;
using Xunit;

namespace CodingAgent.Services.Browser.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class PlaywrightBrowserServiceTests
{
    private readonly Mock<IBrowserPool> _mockBrowserPool;
    private readonly Mock<ILogger<PlaywrightBrowserService>> _mockLogger;
    private readonly BrowserOptions _options;
    private readonly PlaywrightBrowserService _service;

    public PlaywrightBrowserServiceTests()
    {
        _mockBrowserPool = new Mock<IBrowserPool>();
        _mockLogger = new Mock<ILogger<PlaywrightBrowserService>>();
        _options = new BrowserOptions
        {
            Headless = true,
            Timeout = 30000
        };

        var optionsMock = new Mock<IOptions<BrowserOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _service = new PlaywrightBrowserService(
            _mockBrowserPool.Object,
            optionsMock.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task BrowseAsync_ShouldAcquireAndReleasePage()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(
            It.IsAny<string>(),
            It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.ContentAsync()).ReturnsAsync("<html><body>Test</body></html>");
        mockPage.Setup(x => x.TitleAsync()).ReturnsAsync("Test Page");
        mockPage.Setup(x => x.Url).Returns("https://example.com");
        mockPage.Setup(x => x.SetDefaultTimeout(It.IsAny<float>()));

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium"
        };

        // Act
        var result = await _service.BrowseAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("<html><body>Test</body></html>");
        result.Title.Should().Be("Test Page");
        result.Url.Should().Be("https://example.com");
        result.StatusCode.Should().Be(200);
        result.BrowserType.Should().Be("chromium");

        _mockBrowserPool.Verify(
            x => x.AcquirePageAsync("chromium", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    [Fact]
    public async Task BrowseAsync_ShouldUseCustomTimeout_WhenProvided()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(
            It.IsAny<string>(),
            It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.ContentAsync()).ReturnsAsync("<html></html>");
        mockPage.Setup(x => x.TitleAsync()).ReturnsAsync("Test");
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            TimeoutMs = 60000
        };

        // Act
        await _service.BrowseAsync(request);

        // Assert
        mockPage.Verify(x => x.SetDefaultTimeout(60000), Times.Once);
    }

    [Fact]
    public async Task BrowseAsync_ShouldReleasePageOnError()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        
        mockPage.Setup(x => x.GotoAsync(
            It.IsAny<string>(),
            It.IsAny<PageGotoOptions>()))
            .ThrowsAsync(new Exception("Navigation failed"));

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.BrowseAsync(request));

        // Verify page was released even on error
        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    [Fact]
    public async Task BrowseAsync_ShouldThrowTimeoutException_WhenNavigationTimesOut()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        
        mockPage.Setup(x => x.GotoAsync(
            It.IsAny<string>(),
            It.IsAny<PageGotoOptions>()))
            .ThrowsAsync(new TimeoutException("Navigation timeout"));

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium"
        };

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() => _service.BrowseAsync(request));

        // Verify page was released
        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    [Fact]
    public async Task BrowseAsync_ShouldUseNetworkIdleWaitState_WhenEnabled()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        PageGotoOptions? capturedOptions = null;
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(
            It.IsAny<string>(),
            It.IsAny<PageGotoOptions>()))
            .Callback<string, PageGotoOptions>((url, options) => capturedOptions = options)
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.ContentAsync()).ReturnsAsync("<html></html>");
        mockPage.Setup(x => x.TitleAsync()).ReturnsAsync("Test");
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            WaitForNetworkIdle = true
        };

        // Act
        await _service.BrowseAsync(request);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.WaitUntil.Should().Be(WaitUntilState.NetworkIdle);
    }
}
