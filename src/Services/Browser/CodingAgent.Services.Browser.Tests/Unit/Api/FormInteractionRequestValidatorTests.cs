using CodingAgent.Services.Browser.Api.Validators;
using CodingAgent.Services.Browser.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Browser.Tests.Unit.Api;

[Trait("Category", "Unit")]
public class FormInteractionRequestValidatorTests
{
    private readonly FormInteractionRequestValidator _validator;

    public FormInteractionRequestValidatorTests()
    {
        _validator = new FormInteractionRequestValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new FormInteractionRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Fields = new Dictionary<string, string> { { "#username", "test" } }
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
        var request = new FormInteractionRequest
        {
            Url = "",
            BrowserType = "chromium",
            Fields = new Dictionary<string, string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenFieldsIsEmpty()
    {
        // Arrange
        var request = new FormInteractionRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            Fields = new Dictionary<string, string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task Validate_ShouldSucceed_WhenBrowserTypeIsValid(string browserType)
    {
        // Arrange
        var request = new FormInteractionRequest
        {
            Url = "https://example.com",
            BrowserType = browserType,
            Fields = new Dictionary<string, string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("")]
    public async Task Validate_ShouldFail_WhenBrowserTypeIsInvalid(string browserType)
    {
        // Arrange
        var request = new FormInteractionRequest
        {
            Url = "https://example.com",
            BrowserType = browserType,
            Fields = new Dictionary<string, string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BrowserType");
    }
}
