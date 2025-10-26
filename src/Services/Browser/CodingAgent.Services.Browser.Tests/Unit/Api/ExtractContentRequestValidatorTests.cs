using CodingAgent.Services.Browser.Api.Validators;
using CodingAgent.Services.Browser.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Browser.Tests.Unit.Api;

[Trait("Category", "Unit")]
public class ExtractContentRequestValidatorTests
{
    private readonly ExtractContentRequestValidator _validator;

    public ExtractContentRequestValidatorTests()
    {
        _validator = new ExtractContentRequestValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new ExtractContentRequest
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
        var request = new ExtractContentRequest
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

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///path/to/file")]
    public async Task Validate_ShouldFail_WhenUrlSchemeIsNotHttpOrHttps(string url)
    {
        // Arrange
        var request = new ExtractContentRequest
        {
            Url = url,
            BrowserType = "chromium"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(30000)]
    public async Task Validate_ShouldSucceed_WhenTimeoutIsPositive(int timeoutMs)
    {
        // Arrange
        var request = new ExtractContentRequest
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
    public async Task Validate_ShouldFail_WhenTimeoutIsZeroOrNegative(int timeoutMs)
    {
        // Arrange
        var request = new ExtractContentRequest
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
}
