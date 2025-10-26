using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CodingAgent.Services.GitHub.Tests.Integration;

[Trait("Category", "Integration")]
public class PingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PingEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Ping_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ping_ReturnsExpectedContent()
    {
        // Act
        var response = await _client.GetAsync("/ping");
        var content = await response.Content.ReadFromJsonAsync<PingResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Service.Should().Be("GitHub Service");
        content.Status.Should().Be("healthy");
        content.Version.Should().Be("2.0.0");
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }
}

public record PingResponse(string Service, string Status, string Version, DateTime Timestamp);
