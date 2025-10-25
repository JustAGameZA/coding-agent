using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CodingAgent.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// No-op event publisher used for test and local scenarios where a message bus
/// isn't available. Publishes are logged at Debug level and never throw.
/// </summary>
public sealed class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        _logger.LogDebug("[NoOpEventPublisher] Publish {EventType} ({EventId}) skipped", typeof(TEvent).Name, @event.EventId);
        return Task.CompletedTask;
    }

    public Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var count = events is ICollection<TEvent> c ? c.Count : events.Count();
        _logger.LogDebug("[NoOpEventPublisher] Publish batch of {Count} {EventType} skipped", count, typeof(TEvent).Name);
        return Task.CompletedTask;
    }
}
