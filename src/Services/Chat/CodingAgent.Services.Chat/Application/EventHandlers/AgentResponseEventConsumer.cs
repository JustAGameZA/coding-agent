using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace CodingAgent.Services.Chat.Application.EventHandlers;

/// <summary>
/// Consumes AgentResponseEvent from Orchestration Service and broadcasts to users via SignalR
/// </summary>
public class AgentResponseEventConsumer : IConsumer<AgentResponseEvent>
{
    private readonly IConversationService _conversationService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<AgentResponseEventConsumer> _logger;

    public AgentResponseEventConsumer(
        IConversationService conversationService,
        IHubContext<ChatHub> hubContext,
        ILogger<AgentResponseEventConsumer> logger)
    {
        _conversationService = conversationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AgentResponseEvent> context)
    {
        var evt = context.Message;
        
        using var activity = Activity.Current?.Source.StartActivity("ConsumeAgentResponseEvent");
        activity?.SetTag("conversation.id", evt.ConversationId);
        activity?.SetTag("message.id", evt.MessageId);
        activity?.SetTag("tokens.used", evt.TokensUsed);
        activity?.SetTag("model", evt.Model);

        _logger.LogInformation(
            "Received agent response for conversation {ConversationId}, message {MessageId}",
            evt.ConversationId,
            evt.MessageId);

        try
        {
            // 1. Persist agent message (userId = null for agent)
            var message = await _conversationService.AddMessageAsync(
                evt.ConversationId,
                null, // Agent messages have no userId
                evt.Content,
                MessageRole.Assistant);

            _logger.LogInformation(
                "Persisted agent message {MessageId} to conversation {ConversationId}",
                message.Id,
                evt.ConversationId);

            // 2. Broadcast to user via SignalR
            await _hubContext.Clients.Group(evt.ConversationId.ToString())
                .SendAsync("ReceiveMessage", new
                {
                    Id = message.Id,
                    ConversationId = evt.ConversationId,
                    UserId = (Guid?)null,
                    Content = evt.Content,
                    Role = "Assistant",
                    SentAt = message.SentAt,
                    Metadata = new
                    {
                        TokensUsed = evt.TokensUsed,
                        Model = evt.Model
                    }
                });

            // 3. Hide typing indicator
            await _hubContext.Clients.Group(evt.ConversationId.ToString())
                .SendAsync("AgentTyping", false);

            _logger.LogInformation(
                "Broadcast agent message {MessageId} to conversation {ConversationId}",
                message.Id,
                evt.ConversationId);

            activity?.SetTag("broadcast.success", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process agent response for conversation {ConversationId}",
                evt.ConversationId);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            // Send error message to client via SignalR before retrying
            try
            {
                await _hubContext.Clients.Group(evt.ConversationId.ToString())
                    .SendAsync("ReceiveMessage", new
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = evt.ConversationId,
                        UserId = (Guid?)null,
                        Content = $"❌ Error processing response: {ex.Message}",
                        Role = "Assistant",
                        SentAt = DateTime.UtcNow,
                        IsError = true
                    });

                // Hide typing indicator
                await _hubContext.Clients.Group(evt.ConversationId.ToString())
                    .SendAsync("AgentTyping", false);
            }
            catch (Exception signalREx)
            {
                _logger.LogError(signalREx, "Failed to send error message to client for conversation {ConversationId}", evt.ConversationId);
            }
            
            throw; // MassTransit will retry
        }
    }
}
