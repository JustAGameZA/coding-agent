using System.Diagnostics;
using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Services;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;

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
