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
public class AdvancedFeaturesTests
{
    private readonly Mock<IBrowserPool> _mockBrowserPool;
    private readonly Mock<ILogger<PlaywrightBrowserService>> _mockLogger;
    private readonly BrowserOptions _options;
    private readonly PlaywrightBrowserService _service;

    public AdvancedFeaturesTests()
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

    #region Screenshot Tests

    [Fact]
    public async Task CaptureScreenshotAsync_ShouldAcquireAndReleasePage()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        var screenshotBytes = new byte[] { 1, 2, 3, 4 };
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(
            It.IsAny<string>(),
            It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.ScreenshotAsync(It.IsAny<PageScreenshotOptions>()))
            .ReturnsAsync(screenshotBytes);
        mockPage.Setup(x => x.Url).Returns("https://example.com");
        mockPage.Setup(x => x.SetDefaultTimeout(It.IsAny<float>()));

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png"
        };

        // Act
        var result = await _service.CaptureScreenshotAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ImageData.Should().NotBeNullOrEmpty();
        result.Format.Should().Be("png");
        result.Url.Should().Be("https://example.com");
        result.BrowserType.Should().Be("chromium");

        _mockBrowserPool.Verify(
            x => x.AcquirePageAsync("chromium", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    [Fact]
    public async Task CaptureScreenshotAsync_ShouldCaptureFullPage_WhenFullPageIsTrue()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        var screenshotBytes = new byte[] { 1, 2, 3, 4 };
        PageScreenshotOptions? capturedOptions = null;
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.ScreenshotAsync(It.IsAny<PageScreenshotOptions>()))
            .Callback<PageScreenshotOptions>(options => capturedOptions = options)
            .ReturnsAsync(screenshotBytes);
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png",
            FullPage = true
        };

        // Act
        await _service.CaptureScreenshotAsync(request);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.FullPage.Should().BeTrue();
    }

    [Fact]
    public async Task CaptureScreenshotAsync_ShouldCaptureElement_WhenSelectorProvided()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        var mockElement = new Mock<IElementHandle>();
        var screenshotBytes = new byte[] { 1, 2, 3, 4 };
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.QuerySelectorAsync(It.IsAny<string>(), It.IsAny<PageQuerySelectorOptions>()))
            .ReturnsAsync(mockElement.Object);
        mockElement.Setup(x => x.ScreenshotAsync(It.IsAny<ElementHandleScreenshotOptions>()))
            .ReturnsAsync(screenshotBytes);
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png",
            Selector = "#element"
        };

        // Act
        var result = await _service.CaptureScreenshotAsync(request);

        // Assert
        result.Should().NotBeNull();
        mockPage.Verify(x => x.QuerySelectorAsync(It.IsAny<string>(), It.IsAny<PageQuerySelectorOptions>()), Times.Once);
        mockElement.Verify(x => x.ScreenshotAsync(It.IsAny<ElementHandleScreenshotOptions>()), Times.Once);
    }

    [Fact]
    public async Task CaptureScreenshotAsync_ShouldThrow_WhenElementNotFound()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.QuerySelectorAsync(It.IsAny<string>(), It.IsAny<PageQuerySelectorOptions>()))
            .ReturnsAsync((IElementHandle?)null);

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png",
            Selector = "#missing"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CaptureScreenshotAsync(request));

        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    [Fact]
    public async Task CaptureScreenshotAsync_ShouldReleasePageOnError()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ThrowsAsync(new Exception("Navigation failed"));

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CaptureScreenshotAsync(request));

        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    #endregion

    #region Extract Content Tests

    [Fact]
    public async Task ExtractContentAsync_ShouldExtractTextLinksAndImages()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        var mockLink1 = new Mock<IElementHandle>();
        var mockLink2 = new Mock<IElementHandle>();
        var mockImg1 = new Mock<IElementHandle>();
        var mockImg2 = new Mock<IElementHandle>();
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.InnerTextAsync(It.IsAny<string>(), It.IsAny<PageInnerTextOptions>()))
            .ReturnsAsync("Page text content");
        mockPage.Setup(x => x.QuerySelectorAllAsync(It.IsAny<string>()))
            .ReturnsAsync((string selector) =>
            {
                if (selector == "a[href]")
                {
                    return new[] { mockLink1.Object, mockLink2.Object };
                }
                if (selector == "img[src]")
                {
                    return new[] { mockImg1.Object, mockImg2.Object };
                }
                return Array.Empty<IElementHandle>();
            });
        
        mockLink1.Setup(x => x.GetAttributeAsync(It.IsAny<string>())).ReturnsAsync("https://example.com/page1");
        mockLink2.Setup(x => x.GetAttributeAsync(It.IsAny<string>())).ReturnsAsync("https://example.com/page2");
        mockImg1.Setup(x => x.GetAttributeAsync(It.IsAny<string>())).ReturnsAsync("https://example.com/img1.jpg");
        mockImg2.Setup(x => x.GetAttributeAsync(It.IsAny<string>())).ReturnsAsync("https://example.com/img2.png");
        
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new ExtractContentRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            ExtractText = true,
            ExtractLinks = true,
            ExtractImages = true
        };

        // Act
        var result = await _service.ExtractContentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Page text content");
        result.Links.Should().HaveCount(2);
        result.Links.Should().Contain("https://example.com/page1");
        result.Links.Should().Contain("https://example.com/page2");
        result.Images.Should().HaveCount(2);
        result.Images.Should().Contain("https://example.com/img1.jpg");
        result.Images.Should().Contain("https://example.com/img2.png");
        result.Url.Should().Be("https://example.com");

        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    [Fact]
    public async Task ExtractContentAsync_ShouldOnlyExtractText_WhenOnlyTextRequested()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.InnerTextAsync(It.IsAny<string>(), It.IsAny<PageInnerTextOptions>()))
            .ReturnsAsync("Page text content");
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new ExtractContentRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            ExtractText = true,
            ExtractLinks = false,
            ExtractImages = false
        };

        // Act
        var result = await _service.ExtractContentAsync(request);

        // Assert
        result.Text.Should().Be("Page text content");
        result.Links.Should().BeEmpty();
        result.Images.Should().BeEmpty();
        
        mockPage.Verify(x => x.QuerySelectorAllAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Form Interaction Tests

    [Fact]
    public async Task InteractWithFormAsync_ShouldFillFieldsAndSubmit()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.FillAsync(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);
        mockPage.Setup(x => x.ClickAsync(It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);
        mockPage.Setup(x => x.WaitForLoadStateAsync(It.IsAny<LoadState>(), It.IsAny<PageWaitForLoadStateOptions>()))
            .Returns(Task.CompletedTask);
        mockPage.Setup(x => x.ContentAsync()).ReturnsAsync("<html><body>Success</body></html>");
        mockPage.Setup(x => x.TitleAsync()).ReturnsAsync("Success Page");
        mockPage.Setup(x => x.Url).Returns("https://example.com/success");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new FormInteractionRequest
        {
            Url = "https://example.com/form",
            BrowserType = "chromium",
            Fields = new Dictionary<string, string>
            {
                { "#username", "testuser" },
                { "#password", "testpass" }
            },
            SubmitButtonSelector = "#submit",
            WaitForNavigation = true
        };

        // Act
        var result = await _service.InteractWithFormAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Title.Should().Be("Success Page");
        result.Content.Should().Contain("Success");
        result.Url.Should().Be("https://example.com/success");

        mockPage.Verify(x => x.FillAsync("#username", "testuser", null), Times.Once);
        mockPage.Verify(x => x.FillAsync("#password", "testpass", null), Times.Once);
        mockPage.Verify(x => x.ClickAsync("#submit", null), Times.Once);
        mockPage.Verify(x => x.WaitForLoadStateAsync(LoadState.NetworkIdle, It.IsAny<PageWaitForLoadStateOptions>()), Times.Once);
    }

    [Fact]
    public async Task InteractWithFormAsync_ShouldFillFieldsWithoutSubmitting()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.FillAsync(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);
        mockPage.Setup(x => x.ContentAsync()).ReturnsAsync("<html><body>Form</body></html>");
        mockPage.Setup(x => x.TitleAsync()).ReturnsAsync("Form Page");
        mockPage.Setup(x => x.Url).Returns("https://example.com/form");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new FormInteractionRequest
        {
            Url = "https://example.com/form",
            BrowserType = "chromium",
            Fields = new Dictionary<string, string>
            {
                { "#username", "testuser" }
            },
            SubmitButtonSelector = null
        };

        // Act
        var result = await _service.InteractWithFormAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        mockPage.Verify(x => x.FillAsync("#username", "testuser", null), Times.Once);
        mockPage.Verify(x => x.ClickAsync(It.IsAny<string>(), null), Times.Never);
    }

    #endregion

    #region PDF Generation Tests

    [Fact]
    public async Task GeneratePdfAsync_ShouldGeneratePdf()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.PdfAsync(It.IsAny<PagePdfOptions>()))
            .ReturnsAsync(pdfBytes);
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync("chromium", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new PdfRequest
        {
            Url = "https://example.com",
            Format = "A4",
            PrintBackground = true
        };

        // Act
        var result = await _service.GeneratePdfAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.PdfData.Should().NotBeNullOrEmpty();
        result.Url.Should().Be("https://example.com");
        result.SizeBytes.Should().Be(pdfBytes.Length);

        _mockBrowserPool.Verify(
            x => x.AcquirePageAsync("chromium", It.IsAny<CancellationToken>()),
            Times.Once);
        mockPage.Verify(x => x.PdfAsync(It.IsAny<PagePdfOptions>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePdfAsync_ShouldApplyCustomMargins()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        var mockResponse = new Mock<IResponse>();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        PagePdfOptions? capturedOptions = null;
        
        mockResponse.Setup(x => x.Status).Returns(200);
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ReturnsAsync(mockResponse.Object);
        mockPage.Setup(x => x.PdfAsync(It.IsAny<PagePdfOptions>()))
            .Callback<PagePdfOptions>(options => capturedOptions = options)
            .ReturnsAsync(pdfBytes);
        mockPage.Setup(x => x.Url).Returns("https://example.com");

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync("chromium", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new PdfRequest
        {
            Url = "https://example.com",
            MarginTop = "10mm",
            MarginRight = "10mm",
            MarginBottom = "10mm",
            MarginLeft = "10mm"
        };

        // Act
        await _service.GeneratePdfAsync(request);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Margin.Should().NotBeNull();
        capturedOptions.Margin!.Top.Should().Be("10mm");
        capturedOptions.Margin.Right.Should().Be("10mm");
        capturedOptions.Margin.Bottom.Should().Be("10mm");
        capturedOptions.Margin.Left.Should().Be("10mm");
    }

    [Fact]
    public async Task GeneratePdfAsync_ShouldReleasePageOnError()
    {
        // Arrange
        var mockPage = new Mock<IPage>();
        
        mockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()))
            .ThrowsAsync(new Exception("Navigation failed"));

        _mockBrowserPool
            .Setup(x => x.AcquirePageAsync("chromium", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPage.Object);

        var request = new PdfRequest
        {
            Url = "https://example.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GeneratePdfAsync(request));

        _mockBrowserPool.Verify(
            x => x.ReleasePageAsync(mockPage.Object),
            Times.Once);
    }

    #endregion
}
