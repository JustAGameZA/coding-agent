using CodingAgent.Services.Ollama.Infrastructure.CloudApi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CodingAgent.Services.Ollama.Tests.Unit.Infrastructure.CloudApi;

[Trait("Category", "Unit")]
public class MockCloudApiClientTests
{
    private readonly Mock<ILogger<MockCloudApiClient>> _loggerMock;
    private readonly CloudApiOptions _options;
    private readonly MockCloudApiClient _sut;

    public MockCloudApiClientTests()
    {
        _loggerMock = new Mock<ILogger<MockCloudApiClient>>();
        _options = new CloudApiOptions();
        var optionsMock = Options.Create(_options);
        _sut = new MockCloudApiClient(optionsMock, _loggerMock.Object);
    }

    [Fact]
    public void IsConfigured_WhenNoApiKey_ShouldReturnFalse()
    {
        // Arrange
        _options.Provider = "openai";
        _options.ApiKey = null;

        // Act
        var result = _sut.IsConfigured();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenEmptyApiKey_ShouldReturnFalse()
    {
        // Arrange
        _options.Provider = "openai";
        _options.ApiKey = "";

        // Act
        var result = _sut.IsConfigured();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenNoProvider_ShouldReturnFalse()
    {
        // Arrange
        _options.Provider = string.Empty;
        _options.ApiKey = "test-key";

        // Act
        var result = _sut.IsConfigured();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenProviderIsNone_ShouldReturnFalse()
    {
        // Arrange
        _options.Provider = "none";
        _options.ApiKey = "test-key";

        // Act
        var result = _sut.IsConfigured();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        _options.Provider = "openai";
        _options.ApiKey = "sk-test-key-12345";

        // Act
        var result = _sut.IsConfigured();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasTokensAvailableAsync_WhenNotConfigured_ShouldReturnFalse()
    {
        // Arrange
        _options.Provider = "none";
        _options.ApiKey = null;

        // Act
        var result = await _sut.HasTokensAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasTokensAvailableAsync_WhenConfigured_ShouldReturnTrue()
    {
        // Arrange
        _options.Provider = "openai";
        _options.ApiKey = "sk-test-key";

        // Act
        var result = await _sut.HasTokensAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowNotImplementedException()
    {
        // Arrange
        var request = new InferenceRequest
        {
            Model = "gpt-4",
            Prompt = "Test prompt"
        };

        // Act
        Func<Task> act = async () => await _sut.GenerateAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*MockCloudApiClient does not implement actual generation*");
    }
}
