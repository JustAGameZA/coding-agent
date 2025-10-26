using CodingAgent.Services.Browser.Api.Validators;
using CodingAgent.Services.Browser.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Browser.Tests.Unit.Api;

[Trait("Category", "Unit")]
public class ScreenshotRequestValidatorTests
{
    private readonly ScreenshotRequestValidator _validator;

    public ScreenshotRequestValidatorTests()
    {
        _validator = new ScreenshotRequestValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "png"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenUrlIsEmpty()
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "",
            BrowserType = "chromium",
            Format = "png"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    [InlineData("PNG")]
    [InlineData("JPEG")]
    public async Task Validate_ShouldSucceed_WhenFormatIsValid(string format)
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = format
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("gif")]
    [InlineData("webp")]
    [InlineData("")]
    public async Task Validate_ShouldFail_WhenFormatIsInvalid(string format)
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = format
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Validate_ShouldSucceed_WhenQualityIsInRange(int quality)
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "jpeg",
            Quality = quality
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(200)]
    public async Task Validate_ShouldFail_WhenQualityIsOutOfRange(int quality)
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Format = "jpeg",
            Quality = quality
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quality");
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task Validate_ShouldSucceed_WhenBrowserTypeIsValid(string browserType)
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = browserType,
            Format = "png"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("safari")]
    [InlineData("")]
    public async Task Validate_ShouldFail_WhenBrowserTypeIsInvalid(string browserType)
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Url = "https://example.com",
            BrowserType = browserType,
            Format = "png"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BrowserType");
    }
}
