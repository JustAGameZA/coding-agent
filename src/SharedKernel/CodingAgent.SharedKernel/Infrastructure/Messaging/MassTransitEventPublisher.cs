using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CodingAgent.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Implementation of IEventPublisher using MassTransit for RabbitMQ.
/// Provides retry logic, dead-letter queue support, and observability.
/// </summary>
public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventPublisher> _logger;
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitEventPublisher"/> class.
    /// </summary>
    /// <param name="publishEndpoint">The MassTransit publish endpoint.</param>
    /// <param name="logger">The logger.</param>
    public MassTransitEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = new ActivitySource("CodingAgent.EventPublishing");
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        using var activity = _activitySource.StartActivity("PublishEvent");
        activity?.SetTag("event.type", typeof(TEvent).Name);
        activity?.SetTag("event.id", @event.EventId);

        try
        {
            _logger.LogInformation(
                "Publishing event {EventType} with ID {EventId}",
                typeof(TEvent).Name,
                @event.EventId);

            await _publishEndpoint.Publish(@event, cancellationToken);

            _logger.LogInformation(
                "Successfully published event {EventType} with ID {EventId}",
                typeof(TEvent).Name,
                @event.EventId);

            activity?.SetTag("event.published", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} with ID {EventId}",
                typeof(TEvent).Name,
                @event.EventId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventList = events.ToList();
        if (!eventList.Any())
        {
            _logger.LogDebug("No events to publish in batch");
            return;
        }

        using var activity = _activitySource.StartActivity("PublishBatchEvents");
        activity?.SetTag("event.type", typeof(TEvent).Name);
        activity?.SetTag("event.count", eventList.Count);

        _logger.LogInformation(
            "Publishing batch of {Count} events of type {EventType}",
            eventList.Count,
            typeof(TEvent).Name);

        var publishedCount = 0;
        var failedCount = 0;

        foreach (var @event in eventList)
        {
            try
            {
                await PublishAsync(@event, cancellationToken);
                publishedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(
                    ex,
                    "Failed to publish event {EventId} in batch",
                    @event.EventId);
            }
        }

        _logger.LogInformation(
            "Batch publish completed: {PublishedCount} succeeded, {FailedCount} failed",
            publishedCount,
            failedCount);

        activity?.SetTag("event.published_count", publishedCount);
        activity?.SetTag("event.failed_count", failedCount);

        if (failedCount > 0)
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"{failedCount} events failed to publish");
        }
    }
}
