using CodingAgent.SharedKernel.Domain.Events;
using CodingAgent.Services.Orchestration.Domain.Models;
using CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;
using MassTransit;

namespace CodingAgent.Services.Orchestration.Infrastructure.Messaging.Consumers;

public class MessageSentEventConsumer : IConsumer<MessageSentEvent>
{
    private readonly ILogger<MessageSentEventConsumer> _logger;
    private readonly ILlmClient _llmClient;
    private readonly ChatServiceClient _chatServiceClient;

    public MessageSentEventConsumer(
        ILogger<MessageSentEventConsumer> logger,
        ILlmClient llmClient,
        ChatServiceClient chatServiceClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _chatServiceClient = chatServiceClient ?? throw new ArgumentNullException(nameof(chatServiceClient));
    }

    public async Task Consume(ConsumeContext<MessageSentEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[Orchestration] Consumed MessageSentEvent: ConversationId={ConversationId}, MessageId={MessageId}, UserId={UserId}, Role={Role}",
            msg.ConversationId, msg.MessageId, msg.UserId, msg.Role);

        // Only process user messages (not assistant messages)
        if (msg.Role != "User")
        {
            _logger.LogDebug("Skipping non-user message with role {Role}", msg.Role);
            return;
        }

        try
        {
            // Task creation is disabled - all messages get direct responses
            _logger.LogInformation(
                "Task creation is disabled - providing direct response for message {MessageId}",
                msg.MessageId);

            // This is a simple chat message - provide direct response
            _logger.LogInformation(
                "Message {MessageId} is a chat message, providing direct response",
                msg.MessageId);

            // Generate AI response using LLM
            // Use a fast chat model (maps to mistral:latest in Ollama)
            var llmRequest = new LlmRequest
            {
                Model = "mistral:latest", // Use :latest tag for Ollama models
                Messages = new List<LlmMessage>
                {
                    new() { 
                        Role = "system", 
                        Content = "You are a helpful AI assistant. Respond to user messages in a friendly and helpful manner. Keep responses concise and natural." 
                    },
                    new() { 
                        Role = "user", 
                        Content = msg.Content 
                    }
                },
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var llmResponse = await _llmClient.GenerateAsync(llmRequest, context.CancellationToken);

            _logger.LogInformation(
                "AI response generated. Tokens: {Tokens}, Cost: ${Cost:F4}",
                llmResponse.TokensUsed,
                llmResponse.Cost);

            // Post response back to Chat Service
            await _chatServiceClient.PostAgentResponseAsync(
                msg.ConversationId,
                llmResponse.Content,
                context.CancellationToken);

            _logger.LogInformation(
                "Successfully processed MessageSentEvent for conversation {ConversationId}",
                msg.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process MessageSentEvent for conversation {ConversationId}",
                msg.ConversationId);
            // Re-throw to let MassTransit handle retries/error handling
            throw;
        }
    }


    /// <summary>
    /// Extracts a task title from message content.
    /// </summary>
    private static string ExtractTaskTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Untitled Task";
        }

        // Take first sentence or first 100 characters
        var firstSentence = content.Split('.', '!', '?').FirstOrDefault()?.Trim();
        if (!string.IsNullOrWhiteSpace(firstSentence) && firstSentence.Length <= 100)
        {
            return firstSentence;
        }

        // Fallback to first 100 characters
        return content.Length > 100 ? content.Substring(0, 100) + "..." : content;
    }
}
