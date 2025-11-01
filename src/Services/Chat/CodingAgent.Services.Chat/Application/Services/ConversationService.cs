using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Application.Services;

/// <summary>
/// Service for conversation and message operations
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository conversationRepository,
        ChatDbContext dbContext,
        ILogger<ConversationService> logger)
    {
        _conversationRepository = conversationRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Message> AddMessageAsync(
        Guid conversationId,
        Guid? userId,
        string content,
        MessageRole role,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Adding {Role} message to conversation {ConversationId} from user {UserId}",
            role,
            conversationId,
            userId?.ToString() ?? "agent");

        try
        {
            // Validate conversation exists
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, ct);
            if (conversation == null)
            {
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }

            // Create message
            var message = new Message(conversationId, userId, content, role);

            // Add to conversation entity (updates conversation.UpdatedAt)
            conversation.AddMessage(message);

            // Persist message and update conversation
            _dbContext.Messages.Add(message);
            await _conversationRepository.UpdateAsync(conversation, ct);

            // Ensure SaveChangesAsync is called to persist the message
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Message {MessageId} added to conversation {ConversationId}",
                message.Id,
                conversationId);

            return message;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error adding message to conversation {ConversationId}: {Message}", conversationId, dbEx.Message);
            throw new InvalidOperationException($"Database error: {dbEx.Message}", dbEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to conversation {ConversationId}: {Message}", conversationId, ex.Message);
            throw;
        }
    }
}
