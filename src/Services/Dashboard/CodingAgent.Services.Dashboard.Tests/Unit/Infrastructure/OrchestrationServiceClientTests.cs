using System.Diagnostics;
using CodingAgent.Services.Dashboard.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using System.Net;
using System.Text;

namespace CodingAgent.Services.Dashboard.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class OrchestrationServiceClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<OrchestrationServiceClient>> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly OrchestrationServiceClient _client;

    public OrchestrationServiceClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5002")
        };

        _mockLogger = new Mock<ILogger<OrchestrationServiceClient>>();
        _activitySource = new ActivitySource("CodingAgent.Services.Dashboard.Tests");

        _client = new OrchestrationServiceClient(httpClient, _mockLogger.Object, _activitySource);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnMockData()
    {
        // Arrange: return empty task list for any GET
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _client.GetStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TotalTasks.Should().Be(0);
        result.TasksPending.Should().Be(0);
        result.TasksRunning.Should().Be(0);
        result.TasksCompleted.Should().Be(0);
        result.TasksFailed.Should().Be(0);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldReturnEmptyList()
    {
        // Act
        var result = await _client.GetTasksAsync(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTasksAsync_WhenExceptionThrown_ShouldReturnEmptyList()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _client.GetTasksAsync(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
