namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Published by Orchestration Service when AI generates a response.
/// Consumed by Chat Service to broadcast to user via SignalR.
/// </summary>
public record AgentResponseEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the conversation.
    /// </summary>
    public required Guid ConversationId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the message.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Gets the message content from the AI agent.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the timestamp when the response was generated.
    /// </summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    /// Optional metadata: Number of tokens used by the AI model.
    /// </summary>
    public int? TokensUsed { get; init; }

    /// <summary>
    /// Optional metadata: AI model identifier used for generation.
    /// </summary>
    public string? Model { get; init; }
}
