using CodingAgent.Services.Browser.Api.Validators;
using CodingAgent.Services.Browser.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Browser.Tests.Unit.Api;

[Trait("Category", "Unit")]
public class PdfRequestValidatorTests
{
    private readonly PdfRequestValidator _validator;

    public PdfRequestValidatorTests()
    {
        _validator = new PdfRequestValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new PdfRequest
        {
            Url = "https://example.com"
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
        var request = new PdfRequest
        {
            Url = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:8080")]
    public async Task Validate_ShouldSucceed_WhenUrlIsHttpOrHttps(string url)
    {
        // Arrange
        var request = new PdfRequest
        {
            Url = url
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///path/to/file")]
    public async Task Validate_ShouldFail_WhenUrlSchemeIsNotHttpOrHttps(string url)
    {
        // Arrange
        var request = new PdfRequest
        {
            Url = url
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(60000)]
    public async Task Validate_ShouldSucceed_WhenTimeoutIsPositive(int timeoutMs)
    {
        // Arrange
        var request = new PdfRequest
        {
            Url = "https://example.com",
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
        var request = new PdfRequest
        {
            Url = "https://example.com",
            TimeoutMs = timeoutMs
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TimeoutMs");
    }
}
