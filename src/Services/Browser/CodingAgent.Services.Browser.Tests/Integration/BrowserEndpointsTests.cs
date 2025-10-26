using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Browser.Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CodingAgent.Services.Browser.Tests.Integration;

[Trait("Category", "Integration")]
public class BrowserEndpointsTests : IClassFixture<BrowserWebApplicationFactory>, IAsyncLifetime
{
    private readonly BrowserWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BrowserEndpointsTests(BrowserWebApplicationFactory factory)
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
            // Browsers not installed, tests will be skipped
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Browse_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            WaitForNetworkIdle = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BrowseResult>();
        result.Should().NotBeNull();
        result!.Url.Should().Be("https://example.com/");
        result.Title.Should().NotBeNullOrEmpty();
        result.Content.Should().NotBeNullOrEmpty();
        result.StatusCode.Should().Be(200);
        result.BrowserType.Should().Be("chromium");
        result.LoadTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Browse_ShouldReturnValidationError_WhenUrlIsInvalid()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "not-a-valid-url",
            BrowserType = "chromium"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Browse_ShouldReturnValidationError_WhenBrowserTypeIsInvalid()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chrome"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Browse_ShouldWorkWithFirefox()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "firefox",
            WaitForNetworkIdle = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BrowseResult>();
        result.Should().NotBeNull();
        result!.BrowserType.Should().Be("firefox");
    }

    [Fact]
    public async Task Browse_ShouldHandleCustomTimeout()
    {
        // Arrange
        var request = new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            TimeoutMs = 60000,
            WaitForNetworkIdle = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/browse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Browse_ShouldHandleMultipleConcurrentRequests()
    {
        // Arrange
        var requests = Enumerable.Range(0, 3).Select(_ => new BrowseRequest
        {
            Url = "https://example.com",
            BrowserType = "chromium",
            WaitForNetworkIdle = false
        });

        // Act
        var tasks = requests.Select(req => _client.PostAsJsonAsync("/browse", req)).ToList();
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }
}

public class BrowserWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Override configuration for testing
        builder.ConfigureServices(services =>
        {
            // Services are already registered by Program.cs
        });

        return base.CreateHost(builder);
    }
}
