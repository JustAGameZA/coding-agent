using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodingAgent.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Extension methods for configuring MassTransit event publishing with retry logic and dead-letter queue.
/// </summary>
public static class MassTransitPublishingExtensions
{
    /// <summary>
    /// Configures MassTransit message serialization and publishing with retry logic.
    /// </summary>
    /// <param name="cfg">The bus configurator.</param>
    public static void ConfigureEventPublishing(this IBusFactoryConfigurator cfg)
    {
        // Configure retry logic with exponential backoff
        cfg.UseMessageRetry(r =>
        {
            // Retry 3 times with exponential backoff
            r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromMilliseconds(100),
                maxInterval: TimeSpan.FromSeconds(5),
                intervalDelta: TimeSpan.FromMilliseconds(500));

            // Ignore exceptions that shouldn't be retried
            r.Ignore<ArgumentException>();
            r.Ignore<ArgumentNullException>();
            r.Ignore<InvalidOperationException>();
        });
    }

    /// <summary>
    /// Configures RabbitMQ-specific settings for event publishing including dead-letter exchange.
    /// </summary>
    /// <param name="cfg">The RabbitMQ bus configurator.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="hostEnvironment">The host environment.</param>
    public static void ConfigureEventPublishingForRabbitMq(
        this IRabbitMqBusFactoryConfigurator cfg,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        // Configure host
        cfg.ConfigureRabbitMQHost(configuration, hostEnvironment);

        // Configure general event publishing settings
        cfg.ConfigureEventPublishing();

        // Configure dead-letter exchange for failed messages
        cfg.Send<object>(x =>
        {
            x.UseRoutingKeyFormatter(context => context.Message.GetType().Name);
        });

        cfg.Publish<object>(x =>
        {
            x.Durable = true; // Persist messages
            x.AutoDelete = false; // Don't auto-delete exchanges
        });
    }
}
