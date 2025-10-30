using CodingAgent.Services.Chat.Domain.Entities;

namespace CodingAgent.Services.Chat.Domain.Services;

/// <summary>
/// Service for conversation and message operations
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Adds a message to a conversation
    /// </summary>
    /// <param name="conversationId">The conversation identifier</param>
    /// <param name="userId">The user identifier (null for agent messages)</param>
    /// <param name="content">The message content</param>
    /// <param name="role">The message role (User, Assistant, System)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created message</returns>
    Task<Message> AddMessageAsync(
        Guid conversationId,
        Guid? userId,
        string content,
        MessageRole role,
        CancellationToken ct = default);
}
