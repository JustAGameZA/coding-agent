using CodingAgent.Services.Browser.Api.Validators;
using CodingAgent.Services.Browser.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Browser.Tests.Unit.Api;

[Trait("Category", "Unit")]
public class BrowseRequestValidatorTests
{
    private readonly BrowseRequestValidator _validator;

    public BrowseRequestValidatorTests()
    {
        _validator = new BrowseRequestValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium"
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
        var request = new BrowseRequest
        {
            Url = "",
            BrowserType = "chromium"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenUrlIsNotHttpOrHttps()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "ftp://example.com",
            BrowserType = "chromium"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("example.com")]
    [InlineData("//example.com")]
    public async Task Validate_ShouldFail_WhenUrlIsInvalid(string invalidUrl)
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = invalidUrl,
            BrowserType = "chromium"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    [InlineData("Chromium")]
    [InlineData("FIREFOX")]
    public async Task Validate_ShouldSucceed_WhenBrowserTypeIsValid(string browserType)
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = browserType
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("safari")]
    [InlineData("edge")]
    [InlineData("")]
    public async Task Validate_ShouldFail_WhenBrowserTypeIsInvalid(string browserType)
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = browserType
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BrowserType");
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(30000)]
    [InlineData(120000)]
    public async Task Validate_ShouldSucceed_WhenTimeoutIsInRange(int timeoutMs)
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            TimeoutMs = timeoutMs
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(120001)]
    [InlineData(200000)]
    public async Task Validate_ShouldFail_WhenTimeoutIsOutOfRange(int timeoutMs)
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            TimeoutMs = timeoutMs
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TimeoutMs");
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenTimeoutIsNull()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            TimeoutMs = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
