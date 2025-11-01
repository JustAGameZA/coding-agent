using CodingAgent.Services.Chat.Api.Extensions;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace CodingAgent.Services.Chat.Api.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication.
/// Requires JWT authentication via query string (?access_token=...) or Authorization header.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IConversationService _conversationService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IConversationService conversationService,
        IConversationRepository conversationRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<ChatHub> logger)
    {
        _conversationService = conversationService;
        _conversationRepository = conversationRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Joins a conversation group to receive messages for that conversation.
    /// Validates that the user owns the conversation before allowing them to join.
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        try
        {
            var userId = Context.User!.GetUserId();
            
            // Validate conversation ID format
            if (!Guid.TryParse(conversationId, out var conversationGuid))
            {
                _logger.LogWarning("Invalid conversation ID format: {ConversationId} for user {UserId}", conversationId, userId);
                await Clients.Caller.SendAsync("Error", $"Invalid conversation ID: {conversationId}");
                return;
            }

            // Verify conversation exists and user owns it
            var conversation = await _conversationRepository.GetByIdAsync(conversationGuid);
            if (conversation == null)
            {
                _logger.LogWarning("User {UserId} attempted to join non-existent conversation {ConversationId}", userId, conversationId);
                await Clients.Caller.SendAsync("Error", $"Conversation not found: {conversationId}");
                return;
            }

            // Enforce ownership - user must own the conversation
            if (conversation.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to join conversation {ConversationId} owned by user {OwnerUserId}", 
                    userId, conversationId, conversation.UserId);
                await Clients.Caller.SendAsync("Error", $"Access denied: You do not own this conversation");
                return;
            }

            // User owns the conversation - allow them to join
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
            _logger.LogInformation(
                "User {UserId} with connection {ConnectionId} joined conversation {ConversationId}", 
                userId, Context.ConnectionId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining conversation {ConversationId}", conversationId);
            await Clients.Caller.SendAsync("Error", $"An error occurred while joining the conversation: {ex.Message}");
        }
    }

    /// <summary>
    /// Leaves a conversation group to stop receiving messages for that conversation.
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation(
            $"User {Context.User?.GetUserId()} with connection {Context.ConnectionId} left conversation {conversationId}");
    }

    /// <summary>
    /// Sends a message to the conversation and triggers AI agent processing.
    /// </summary>
    public async Task SendMessage(string conversationId, string content)
    {
        using var activity = Activity.Current?.Source.StartActivity("ChatHub.SendMessage");
        activity?.SetTag("conversation.id", conversationId);
        activity?.SetTag("content.length", content.Length);

        try
        {
            var userId = Context.User!.GetUserId();
            Guid conversationGuid;

            // Validate conversation ID format
            try
            {
                conversationGuid = Guid.Parse(conversationId);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid conversation ID format: {ConversationId}", conversationId);
                await SendErrorMessage(conversationId, $"Invalid conversation ID format: {conversationId}");
                activity?.SetStatus(ActivityStatusCode.Error, "Invalid conversation ID format");
                return;
            }

            // Verify conversation exists and user owns it before sending message
            var conversation = await _conversationRepository.GetByIdAsync(conversationGuid);
            if (conversation == null)
            {
                _logger.LogWarning("User {UserId} attempted to send message to non-existent conversation {ConversationId}", userId, conversationId);
                await SendErrorMessage(conversationId, $"Conversation not found: {conversationId}");
                activity?.SetStatus(ActivityStatusCode.Error, "Conversation not found");
                return;
            }

            // Enforce ownership - user must own the conversation
            if (conversation.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to send message to conversation {ConversationId} owned by user {OwnerUserId}", 
                    userId, conversationId, conversation.UserId);
                await SendErrorMessage(conversationId, "Access denied: You do not own this conversation");
                activity?.SetStatus(ActivityStatusCode.Error, "Access denied: User does not own conversation");
                return;
            }

            // 1. Persist user message
            Message message;
            try
            {
                message = await _conversationService.AddMessageAsync(
                    conversationGuid,
                    userId,
                    content,
                    MessageRole.User);

                _logger.LogInformation(
                    "User {UserId} sent message {MessageId} to conversation {ConversationId}",
                    userId,
                    message.Id,
                    conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist message in conversation {ConversationId}", conversationId);
                await SendErrorMessage(conversationId, $"Failed to save message: {ex.Message}");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return;
            }

            // 2. Echo message back to user (optimistic UI)
            try
            {
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
                {
                    Id = message.Id,
                    ConversationId = conversationGuid,
                    UserId = userId,
                    Content = content,
                    Role = "User",
                    SentAt = message.SentAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send user message back to client in conversation {ConversationId}", conversationId);
                // Continue processing even if echo fails
            }

            // 3. Show "agent typing" indicator
            try
            {
                await Clients.Group(conversationId).SendAsync("AgentTyping", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send typing indicator in conversation {ConversationId}", conversationId);
                // Continue processing even if typing indicator fails
            }

            // 4. Publish event for Orchestration Service
            try
            {
                await _publishEndpoint.Publish(new MessageSentEvent
                {
                    ConversationId = conversationGuid,
                    MessageId = message.Id,
                    UserId = userId,
                    Content = content,
                    Role = "User",
                    SentAt = message.SentAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish MessageSentEvent for conversation {ConversationId}", conversationId);
                // Send error message to client about processing failure
                await SendErrorMessage(conversationId, $"Failed to process message: {ex.Message}");
                // Hide typing indicator
                await Clients.Group(conversationId).SendAsync("AgentTyping", false);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return;
            }

            activity?.SetTag("message.id", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SendMessage for conversation {ConversationId}", conversationId);
            await SendErrorMessage(conversationId, $"An unexpected error occurred: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }

    /// <summary>
    /// Sends an error message to the client via SignalR.
    /// </summary>
    private async Task SendErrorMessage(string conversationId, string errorMessage)
    {
        try
        {
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = (Guid?)null,
                Content = $"❌ Error: {errorMessage}",
                Role = "Assistant",
                SentAt = DateTime.UtcNow,
                IsError = true
            });

            // Hide typing indicator
            await Clients.Group(conversationId).SendAsync("AgentTyping", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error message to client in conversation {ConversationId}", conversationId);
            // If we can't send the error message, at least log it
        }
    }
}
