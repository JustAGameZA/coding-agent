using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CodingAgent.SharedKernel.Abstractions;
using Moq;
using CodingAgent.SharedKernel.Domain.Events;

namespace CodingAgent.Services.GitHub.Tests.Integration;

[Trait("Category", "Integration")]
public class WebhookEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private const string WebhookSecret = "test-webhook-secret";

    public WebhookEndpointTests(WebApplicationFactory<Program> factory)
    {
        // Create a custom factory with mocked dependencies and test configuration
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GitHub:WebhookSecret"] = WebhookSecret,
                    ["GitHub:Token"] = "",
                    ["RabbitMQ:Host"] = "localhost",
                    ["RabbitMQ:Username"] = "guest",
                    ["RabbitMQ:Password"] = "guest"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Override the event publisher with a mock
                var mockEventPublisher = new Mock<IEventPublisher>();
                mockEventPublisher
                    .Setup(x => x.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                services.AddScoped<IEventPublisher>(_ => mockEventPublisher.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task WebhookEndpoint_WithoutSignature_ReturnsUnauthorized()
    {
        // Arrange
        var payload = @"{""test"":""payload""}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/webhooks/github", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebhookEndpoint_WithInvalidSignature_ReturnsUnauthorized()
    {
        // Arrange
        var payload = @"{""test"":""payload""}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/github")
        {
            Content = content
        };
        request.Headers.Add("X-Hub-Signature-256", "sha256=invalidsignature");
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebhookEndpoint_PushEvent_WithValidSignature_ReturnsAccepted()
    {
        // Arrange
        var pushPayload = new
        {
            @ref = "refs/heads/main",
            after = "abc123def456",
            repository = new
            {
                name = "test-repo",
                full_name = "testowner/test-repo",
                owner = new { login = "testowner", id = 123 }
            },
            sender = new { login = "testuser", id = 456 },
            head_commit = new
            {
                id = "abc123def456",
                message = "Test commit",
                timestamp = DateTime.UtcNow,
                url = "https://github.com/testowner/test-repo/commit/abc123def456",
                author = new
                {
                    name = "Test Author",
                    email = "test@example.com",
                    username = "testuser"
                }
            }
        };

        var payload = JsonSerializer.Serialize(pushPayload);
        var signature = GenerateSignature(payload, WebhookSecret);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/github")
        {
            Content = content
        };
        request.Headers.Add("X-Hub-Signature-256", signature);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task WebhookEndpoint_PullRequestEvent_WithValidSignature_ReturnsAccepted()
    {
        // Arrange
        var prPayload = new
        {
            action = "opened",
            number = 42,
            repository = new
            {
                name = "test-repo",
                full_name = "testowner/test-repo",
                owner = new { login = "testowner", id = 123 }
            },
            sender = new { login = "testuser", id = 456 },
            pull_request = new
            {
                id = 789,
                number = 42,
                title = "Test PR",
                state = "open",
                html_url = "https://github.com/testowner/test-repo/pull/42",
                user = new { login = "prauthor", id = 999 },
                merged = false,
                merged_at = (DateTime?)null
            }
        };

        var payload = JsonSerializer.Serialize(prPayload);
        var signature = GenerateSignature(payload, WebhookSecret);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/github")
        {
            Content = content
        };
        request.Headers.Add("X-Hub-Signature-256", signature);
        request.Headers.Add("X-GitHub-Event", "pull_request");
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task WebhookEndpoint_IssueEvent_WithValidSignature_ReturnsAccepted()
    {
        // Arrange
        var issuePayload = new
        {
            action = "opened",
            repository = new
            {
                name = "test-repo",
                full_name = "testowner/test-repo",
                owner = new { login = "testowner", id = 123 }
            },
            sender = new { login = "testuser", id = 456 },
            issue = new
            {
                id = 789,
                number = 10,
                title = "Test Issue",
                state = "open",
                html_url = "https://github.com/testowner/test-repo/issues/10",
                user = new { login = "issueauthor", id = 999 },
                body = "Test issue body"
            }
        };

        var payload = JsonSerializer.Serialize(issuePayload);
        var signature = GenerateSignature(payload, WebhookSecret);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/github")
        {
            Content = content
        };
        request.Headers.Add("X-Hub-Signature-256", signature);
        request.Headers.Add("X-GitHub-Event", "issues");
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task WebhookEndpoint_UnsupportedEvent_ReturnsAccepted()
    {
        // Arrange
        var payload = @"{""test"":""payload""}";
        var signature = GenerateSignature(payload, WebhookSecret);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/github")
        {
            Content = content
        };
        request.Headers.Add("X-Hub-Signature-256", signature);
        request.Headers.Add("X-GitHub-Event", "star"); // Unsupported event
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert - should still return Accepted even for unsupported events
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task WebhookEndpoint_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var payload = @"{invalid json}";
        var signature = GenerateSignature(payload, WebhookSecret);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/github")
        {
            Content = content
        };
        request.Headers.Add("X-Hub-Signature-256", signature);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string GenerateSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
