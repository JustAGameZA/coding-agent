using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Browser.Domain.Models;
using FluentAssertions;
using Xunit;
using Xunit.Sdk;

namespace CodingAgent.Services.Browser.Tests.Integration;

[Trait("Category", "Integration")]
public class AdvancedFeaturesEndpointsTests : IClassFixture<BrowserWebApplicationFactory>, IAsyncLifetime
{
    private readonly BrowserWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdvancedFeaturesEndpointsTests(BrowserWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Install Playwright browsers if needed
        try
        {
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await playwright.Chromium.LaunchAsync();
            playwright.Dispose();
        }
        catch
        {
            // Browsers not installed, skip suite
            throw new SkipException("Playwright browsers not installed");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Screenshot Tests

    [Fact]
    public async Task Screenshot_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png",
            FullPage = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/screenshot", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScreenshotResult>();
        result.Should().NotBeNull();
        result!.ImageData.Should().NotBeNullOrEmpty();
        result.Format.Should().Be("png");
        result.Url.Should().Be("https://example.com/");
        result.BrowserType.Should().Be("chromium");
        result.DurationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Screenshot_ShouldCaptureFullPage()
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png",
            FullPage = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/screenshot", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScreenshotResult>();
        result.Should().NotBeNull();
        result!.ImageData.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Screenshot_ShouldReturnValidationError_WhenFormatIsInvalid()
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "gif"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/screenshot", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Extract Content Tests

    [Fact]
    public async Task ExtractContent_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new ExtractContentRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            ExtractText = true,
            ExtractLinks = true,
            ExtractImages = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/extract", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExtractedContent>();
        result.Should().NotBeNull();
        result!.Text.Should().NotBeNullOrEmpty();
        result.Links.Should().NotBeNull();
        result.Images.Should().NotBeNull();
        result.Url.Should().Be("https://example.com/");
        result.BrowserType.Should().Be("chromium");
        result.DurationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExtractContent_ShouldExtractOnlyText_WhenOnlyTextRequested()
    {
        // Arrange
        var request = new ExtractContentRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            ExtractText = true,
            ExtractLinks = false,
            ExtractImages = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/extract", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExtractedContent>();
        result.Should().NotBeNull();
        result!.Text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExtractContent_ShouldReturnValidationError_WhenUrlIsInvalid()
    {
        // Arrange
        var request = new ExtractContentRequest
        {
            Url = "not-a-url",
            BrowserType = "chromium"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/extract", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Form Interaction Tests

    [Fact]
    public async Task InteractWithForm_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new FormInteractionRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Fields = new Dictionary<string, string>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/interact", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FormInteractionResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Url.Should().Be("https://example.com/");
        result.Title.Should().NotBeNullOrEmpty();
        result.Content.Should().NotBeNullOrEmpty();
        result.BrowserType.Should().Be("chromium");
        result.DurationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InteractWithForm_ShouldReturnValidationError_WhenBrowserTypeIsInvalid()
    {
        // Arrange
        var request = new FormInteractionRequest
        {
            Url = "https://example.com",
            BrowserType = "safari",
            Fields = new Dictionary<string, string>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/interact", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PDF Generation Tests

    [Fact]
    public async Task GeneratePdf_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new PdfRequest
        {
            Url = "https://example.com",
            PrintBackground = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PdfResult>();
        result.Should().NotBeNull();
        result!.PdfData.Should().NotBeNullOrEmpty();
        result.Url.Should().Be("https://example.com/");
        result.DurationMs.Should().BeGreaterThan(0);
        result.SizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GeneratePdf_ShouldApplyCustomMargins()
    {
        // Arrange
        var request = new PdfRequest
        {
            Url = "https://example.com",
            MarginTop = "10mm",
            MarginRight = "10mm",
            MarginBottom = "10mm",
            MarginLeft = "10mm",
            PrintBackground = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PdfResult>();
        result.Should().NotBeNull();
        result!.PdfData.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GeneratePdf_ShouldReturnValidationError_WhenUrlIsEmpty()
    {
        // Arrange
        var request = new PdfRequest
        {
            Url = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse/pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
