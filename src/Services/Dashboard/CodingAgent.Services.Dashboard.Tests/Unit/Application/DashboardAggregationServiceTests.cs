using System.Diagnostics;
using CodingAgent.Services.Dashboard.Application.DTOs;
using CodingAgent.Services.Dashboard.Application.Services;
using CodingAgent.Services.Dashboard.Infrastructure.Caching;
using CodingAgent.Services.Dashboard.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodingAgent.Services.Dashboard.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class DashboardAggregationServiceTests
{
    private readonly Mock<ChatServiceClient> _mockChatClient;
    private readonly Mock<OrchestrationServiceClient> _mockOrchestrationClient;
    private readonly Mock<IDashboardCacheService> _mockCache;
    private readonly Mock<ILogger<DashboardAggregationService>> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly DashboardAggregationService _service;

    public DashboardAggregationServiceTests()
    {
        var mockHttpClient = new HttpClient();
        var mockHttpLogger1 = new Mock<ILogger<ChatServiceClient>>();
        var mockHttpLogger2 = new Mock<ILogger<OrchestrationServiceClient>>();
        
        _activitySource = new ActivitySource("CodingAgent.Services.Dashboard.Tests");
        
        _mockChatClient = new Mock<ChatServiceClient>(mockHttpClient, mockHttpLogger1.Object, _activitySource);
        _mockOrchestrationClient = new Mock<OrchestrationServiceClient>(mockHttpClient, mockHttpLogger2.Object, _activitySource);
        _mockCache = new Mock<IDashboardCacheService>();
        _mockLogger = new Mock<ILogger<DashboardAggregationService>>();

        _service = new DashboardAggregationService(
            _mockChatClient.Object,
            _mockOrchestrationClient.Object,
            _mockCache.Object,
            _mockLogger.Object,
            _activitySource);
    }

    [Fact]
    public async Task GetStatsAsync_WhenCacheHit_ShouldReturnCachedData()
    {
        // Arrange
        var cachedStats = new DashboardStatsDto
        {
            TotalConversations = 10,
            TotalMessages = 50,
            TotalTasks = 5,
            LastUpdated = DateTime.UtcNow
        };

        _mockCache.Setup(x => x.GetAsync<DashboardStatsDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedStats);

        // Act
        var result = await _service.GetStatsAsync();

        // Assert
        result.Should().BeEquivalentTo(cachedStats);
        _mockChatClient.Verify(x => x.GetStatsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockOrchestrationClient.Verify(x => x.GetStatsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStatsAsync_WhenCacheMiss_ShouldAggregateFromServices()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<DashboardStatsDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardStatsDto?)null);

        var chatStats = new ChatStatsDto { TotalConversations = 10, TotalMessages = 50 };
        var orchestrationStats = new OrchestrationStatsDto
        {
            TotalTasks = 5,
            TasksPending = 2,
            TasksRunning = 1,
            TasksCompleted = 2,
            TasksFailed = 0
        };

        _mockChatClient.Setup(x => x.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatStats);
        _mockOrchestrationClient.Setup(x => x.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestrationStats);

        // Act
        var result = await _service.GetStatsAsync();

        // Assert
        result.TotalConversations.Should().Be(10);
        result.TotalMessages.Should().Be(50);
        result.TotalTasks.Should().Be(5);
        result.TasksPending.Should().Be(2);
        result.TasksRunning.Should().Be(1);
        result.TasksCompleted.Should().Be(2);
        result.TasksFailed.Should().Be(0);

        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<DashboardStatsDto>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatsAsync_WhenServiceReturnsNull_ShouldUseZeroValues()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<DashboardStatsDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardStatsDto?)null);

        _mockChatClient.Setup(x => x.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatStatsDto?)null);
        _mockOrchestrationClient.Setup(x => x.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrchestrationStatsDto?)null);

        // Act
        var result = await _service.GetStatsAsync();

        // Assert
        result.TotalConversations.Should().Be(0);
        result.TotalMessages.Should().Be(0);
        result.TotalTasks.Should().Be(0);
    }

    [Fact]
    public async Task GetTasksAsync_WhenCacheHit_ShouldReturnCachedData()
    {
        // Arrange
        var cachedTasks = new List<EnrichedTaskDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Task 1", Status = "Pending" }
        };

        _mockCache.Setup(x => x.GetAsync<List<EnrichedTaskDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTasks);

        // Act
        var result = await _service.GetTasksAsync();

        // Assert
        result.Should().BeEquivalentTo(cachedTasks);
        _mockOrchestrationClient.Verify(x => x.GetTasksAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTasksAsync_WhenCacheMiss_ShouldFetchFromOrchestration()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<List<EnrichedTaskDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<EnrichedTaskDto>?)null);

        var tasks = new List<TaskDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Description 1",
                Type = "Feature",
                Complexity = "Medium",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockOrchestrationClient.Setup(x => x.GetTasksAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetTasksAsync(1, 20);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Task 1");
        result[0].ExecutionCount.Should().Be(0);

        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<EnrichedTaskDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActivityAsync_WhenCacheHit_ShouldReturnCachedData()
    {
        // Arrange
        var cachedEvents = new List<ActivityEventDto>
        {
            new() { Id = Guid.NewGuid(), Type = "TaskCreated", Description = "New task", Timestamp = DateTime.UtcNow }
        };

        _mockCache.Setup(x => x.GetAsync<List<ActivityEventDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedEvents);

        // Act
        var result = await _service.GetActivityAsync(50);

        // Assert
        result.Should().BeEquivalentTo(cachedEvents);
    }

    [Fact]
    public async Task GetActivityAsync_WhenCacheMiss_ShouldReturnEmptyList()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<List<ActivityEventDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ActivityEventDto>?)null);

        // Act
        var result = await _service.GetActivityAsync(50);

        // Assert
        result.Should().BeEmpty();

        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<ActivityEventDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
