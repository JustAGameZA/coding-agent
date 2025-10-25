using CodingAgent.SharedKernel.Domain.Events;
using CodingAgent.SharedKernel.Domain.ValueObjects;
using CodingAgent.SharedKernel.Infrastructure.Messaging;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CodingAgent.SharedKernel.Tests.Unit.Infrastructure.Messaging;

[Trait("Category", "Unit")]
public class MassTransitEventPublisherTests
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventPublisher> _logger;
    private readonly MassTransitEventPublisher _sut;

    public MassTransitEventPublisherTests()
    {
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<MassTransitEventPublisher>>();
        _sut = new MassTransitEventPublisher(_publishEndpoint, _logger);
    }

    [Fact]
    public async Task PublishAsync_WithValidEvent_ShouldCompleteSuccessfully()
    {
        // Arrange
        var @event = new TaskCreatedEvent
        {
            TaskId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Description = "Test task",
            TaskType = TaskType.BugFix,
            Complexity = TaskComplexity.Simple
        };

        // Act
        var act = async () => await _sut.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        TaskCreatedEvent? @event = null;

        // Act
        var act = async () => await _sut.PublishAsync(@event!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishBatchAsync_WithMultipleEvents_ShouldCompleteSuccessfully()
    {
        // Arrange
        var events = new[]
        {
            new TaskStartedEvent
            {
                TaskId = Guid.NewGuid(),
                TaskType = TaskType.BugFix,
                Complexity = TaskComplexity.Simple,
                Strategy = ExecutionStrategy.SingleShot,
                UserId = Guid.NewGuid()
            },
            new TaskStartedEvent
            {
                TaskId = Guid.NewGuid(),
                TaskType = TaskType.Feature,
                Complexity = TaskComplexity.Medium,
                Strategy = ExecutionStrategy.Iterative,
                UserId = Guid.NewGuid()
            }
        };

        // Act
        var act = async () => await _sut.PublishBatchAsync(events);

        // Assert - should not throw
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishBatchAsync_WithEmptyCollection_ShouldNotPublishAnything()
    {
        // Arrange
        var events = Array.Empty<TaskCreatedEvent>();

        // Act
        var act = async () => await _sut.PublishBatchAsync(events);

        // Assert - should complete without error
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishBatchAsync_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<TaskCreatedEvent>? events = null;

        // Act
        var act = async () => await _sut.PublishBatchAsync(events!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullPublishEndpoint_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new MassTransitEventPublisher(null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("publishEndpoint");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new MassTransitEventPublisher(_publishEndpoint, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
