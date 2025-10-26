using System.Net;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Ollama.Tests.Integration;

/// <summary>
/// Integration tests for Ollama service endpoints
/// </summary>
[Collection("OllamaServiceCollection")]
[Trait("Category", "Integration")]
public class OllamaServiceIntegrationTests
{
    private readonly OllamaServiceFixture _fixture;

    public OllamaServiceIntegrationTests(OllamaServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ServiceRoot_ShouldReturnOk()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Ollama Service");
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task HardwareDetection_ShouldReturnProfile()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/api/hardware");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("gpuType");
        content.Should().Contain("tier");
    }

    [Fact]
    public async Task ModelRecommendations_ShouldReturnModels()
    {
        // Act
        var response = await _fixture.Client.PostAsync("/api/hardware/models", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("hardware");
        content.Should().Contain("recommendedModels");
    }
}
