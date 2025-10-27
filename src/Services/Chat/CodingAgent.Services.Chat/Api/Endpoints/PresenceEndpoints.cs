using CodingAgent.Services.Chat.Infrastructure.Persistence;
using CodingAgent.Services.Chat.Domain.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CodingAgent.Services.Chat.Api.Endpoints;

/// <summary>
/// Presence endpoints for retrieving online/offline state of conversation participants
/// </summary>
public static class PresenceEndpoints
{
    private static readonly ActivitySource ActivitySource = new("chat-service");

    public static void MapPresenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/presence")
            .WithTags("Presence")
            .WithOpenApi();

        group.MapGet("{conversationId:guid}", GetConversationPresence)
            .RequireAuthorization()
            .WithName("GetConversationPresence")
            .WithSummary("Get presence for users in a conversation")
            .WithDescription("Returns an array of { userId, isOnline, lastSeenUtc } for conversation participants. Participants are approximated from recent messages. TODO: replace with repository-backed participant list when available.")
            .Produces<List<ConversationUserPresence>>();
    }

    private static async Task<IResult> GetConversationPresence(
        Guid conversationId,
        ChatDbContext db,
        IPresenceService presence,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("GetConversationPresence");
        activity?.SetTag("conversation.id", conversationId);

        // Approximate participants by distinct user IDs from recent messages
        var userIds = await db.Messages
            .Where(m => m.ConversationId == conversationId && m.UserId.HasValue)
            .OrderByDescending(m => m.SentAt)
            .Select(m => m.UserId!.Value)
            .Distinct()
            .Take(50)
            .ToListAsync(ct);

        logger.LogInformation("Computed {Count} participant(s) for conversation {ConversationId} from recent messages", userIds.Count, conversationId);

        var tasks = userIds.Select(async uid =>
        {
            var uidString = uid.ToString();
            var online = await presence.IsUserOnlineAsync(uidString, ct);
            var lastSeen = await presence.GetLastSeenAsync(uidString, ct);
            return new ConversationUserPresence(uid, online, lastSeen);
        });

        var presences = await Task.WhenAll(tasks);
        return Results.Ok(presences.ToList());
    }
}

/// <summary>
/// Presence payload for a conversation participant
/// </summary>
/// <param name="userId">User identifier</param>
/// <param name="isOnline">Is user currently online</param>
/// <param name="lastSeenUtc">Last seen timestamp in UTC</param>
public readonly record struct ConversationUserPresence(Guid userId, bool isOnline, DateTime? lastSeenUtc);
