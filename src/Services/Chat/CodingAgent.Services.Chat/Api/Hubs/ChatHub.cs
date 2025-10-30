using CodingAgent.Services.Chat.Api.Extensions;
using CodingAgent.Services.Chat.Domain.Entities;
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
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IConversationService conversationService,
        IPublishEndpoint publishEndpoint,
        ILogger<ChatHub> logger)
    {
        _conversationService = conversationService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Joins a conversation group to receive messages for that conversation.
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation(
            $"User {Context.User?.GetUserId()} with connection {Context.ConnectionId} joined conversation {conversationId}");
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

        var userId = Context.User!.GetUserId();
        var conversationGuid = Guid.Parse(conversationId);

        // 1. Persist user message
        var message = await _conversationService.AddMessageAsync(
            conversationGuid,
            userId,
            content,
            MessageRole.User);

        _logger.LogInformation(
            "User {UserId} sent message {MessageId} to conversation {ConversationId}",
            userId,
            message.Id,
            conversationId);

        // 2. Echo message back to user (optimistic UI)
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            ConversationId = conversationGuid,
            UserId = userId,
            Content = content,
            Role = "User",
            SentAt = message.SentAt
        });

        // 3. Show "agent typing" indicator
        await Clients.Group(conversationId).SendAsync("AgentTyping", true);

        // 4. Publish event for Orchestration Service
        await _publishEndpoint.Publish(new MessageSentEvent
        {
            ConversationId = conversationGuid,
            MessageId = message.Id,
            UserId = userId,
            Content = content,
            Role = "User",
            SentAt = message.SentAt
        });

        activity?.SetTag("message.id", message.Id);
    }
}
