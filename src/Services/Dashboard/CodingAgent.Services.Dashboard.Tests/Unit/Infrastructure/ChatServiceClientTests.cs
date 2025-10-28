using System.Diagnostics;
using CodingAgent.Services.Dashboard.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace CodingAgent.Services.Dashboard.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class ChatServiceClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<ChatServiceClient>> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly ChatServiceClient _client;

    public ChatServiceClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        _mockLogger = new Mock<ILogger<ChatServiceClient>>();
        _activitySource = new ActivitySource("CodingAgent.Services.Dashboard.Tests");

        _client = new ChatServiceClient(httpClient, _mockLogger.Object, _activitySource);
    }

    [Fact]
    public async Task GetStatsAsync_WhenSuccessful_ShouldReturnStats()
    {
        // Arrange
        var responseContent = """
        [
            {"id": "00000000-0000-0000-0000-000000000001", "title": "Chat 1", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z"},
            {"id": "00000000-0000-0000-0000-000000000002", "title": "Chat 2", "createdAt": "2024-01-01T00:00:00Z", "updatedAt": "2024-01-01T00:00:00Z"}
        ]
        """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _client.GetStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TotalConversations.Should().Be(2);
        result.TotalMessages.Should().Be(10); // 2 conversations * 5 average
    }

    [Fact]
    public async Task GetStatsAsync_WhenServiceReturnsError_ShouldReturnNull()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError
            });

        // Act
        var result = await _client.GetStatsAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_WhenExceptionThrown_ShouldReturnNull()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _client.GetStatsAsync();

        // Assert
        result.Should().BeNull();
    }
}
