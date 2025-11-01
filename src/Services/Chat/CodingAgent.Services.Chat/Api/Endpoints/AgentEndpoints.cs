using System.Diagnostics;
using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Api.Endpoints;

/// <summary>
/// Endpoints used by the Orchestration Service to post AI agent responses.
/// </summary>
public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations")
            .WithTags("Agent")
            .WithOpenApi()
            .RequireAuthorization("OrchestrationService"); // Only Orchestration Service should call this

        group.MapPost("{id:guid}/agent-response", PostAgentResponse)
            .WithName("PostAgentResponse")
            .WithSummary("Post AI agent response to a conversation")
            .WithDescription("Called by the Orchestration Service after AI processing completes. Persists the agent's message and broadcasts it to the user via SignalR.")
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // Service-to-service endpoint for getting conversation history (bypasses user ownership check)
        group.MapGet("{id:guid}/messages/history", GetConversationHistory)
            .WithName("GetConversationHistory")
            .WithSummary("Get conversation history for service-to-service calls")
            .WithDescription("Called by Orchestration Service to get conversation history for context. Bypasses user ownership check.")
            .Produces<PagedMessagesResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> PostAgentResponse(
        Guid id,
        AgentResponseRequest request,
        IConversationService conversationService,
        IHubContext<ChatHub> hubContext,
        IValidator<AgentResponseRequest> validator,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("AgentEndpoints.PostAgentResponse");
        activity?.SetTag("conversation.id", id);

        // Validate request
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            logger.LogWarning("Agent response validation failed: {Errors}", string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));
            return Results.ValidationProblem(validation.ToDictionary());
        }

        // Ensure conversation exists by attempting to add the message
        try
        {
            var message = await conversationService.AddMessageAsync(
                id,
                null, // Agent messages have no userId
                request.Content,
                MessageRole.Assistant,
                ct);

            // Broadcast agent message via SignalR
            await hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveMessage", new
            {
                Id = message.Id,
                ConversationId = id,
                UserId = (Guid?)null,
                Content = message.Content,
                Role = message.Role.ToString(),
                SentAt = message.SentAt
            }, ct);

            // Hide typing indicator
            await hubContext.Clients.Group(id.ToString()).SendAsync("AgentTyping", false, ct);

            logger.LogInformation("Agent response {MessageId} persisted and broadcast for conversation {ConversationId}", message.Id, id);
            return Results.Accepted();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Agent response attempted for non-existent conversation {ConversationId}", id);
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetConversationHistory(
        Guid id,
        IConversationRepository repository,
        ChatDbContext dbContext,
        ILogger<Program> logger,
        int limit = 10,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting conversation history for service call: conversation {ConversationId} (limit: {Limit})", id, limit);

        // Verify conversation exists
        var conversation = await repository.GetByIdAsync(id, ct);
        if (conversation is null)
        {
            logger.LogWarning("Conversation {ConversationId} not found", id);
            return Results.NotFound();
        }

        // Query messages from database (service-to-service call, no user ownership check)
        var messages = await dbContext.Messages
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.SentAt) // Get most recent messages first
            .Take(Math.Min(limit, 100)) // Max 100
            .OrderBy(m => m.SentAt) // Then order by oldest first for context
            .ToListAsync(ct);

        // Convert to DTOs
        var messageDtos = messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role.ToString(),
            Content = m.Content,
            SentAt = m.SentAt,
            Attachments = new List<AttachmentDto>()
        }).ToList();

        var response = new PagedMessagesResponse
        {
            Items = messageDtos,
            NextCursor = null // Not needed for service calls
        };

        logger.LogInformation("Returned {Count} messages for conversation {ConversationId}", messageDtos.Count, id);
        return Results.Ok(response);
    }
}

/// <summary>
/// Request payload for agent response.
/// </summary>
/// <param name="Content">Agent response text</param>
public record AgentResponseRequest(string Content);

public sealed class AgentResponseRequestValidator : AbstractValidator<AgentResponseRequest>
{
    public AgentResponseRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(10_000)
            .WithMessage("Agent response content must be between 1 and 10000 characters");
    }
}
