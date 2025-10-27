using CodingAgent.Services.Chat.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CodingAgent.Services.Chat.Api.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication.
/// Requires JWT authentication via query string (?access_token=...) or Authorization header.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IPresenceService _presenceService;

    public ChatHub(ILogger<ChatHub> logger, IPresenceService presenceService)
    {
        _logger = logger;
        _presenceService = presenceService;
    }

    /// <summary>
    /// Gets the authenticated user ID from the JWT token claims.
    /// </summary>
    private string UserId => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.FindFirst("sub")?.Value
        ?? "anonymous";

    public override async Task OnConnectedAsync()
    {
        // Mark user as online
        await _presenceService.SetUserOnlineAsync(UserId, Context.ConnectionId);
        
        // Broadcast presence change to all clients
        await Clients.All.SendAsync("UserPresenceChanged", new
        {
            userId = UserId,
            isOnline = true,
            lastSeenUtc = (DateTime?)null
        });
        
        _logger.LogInformation("User {UserId} connected to chat hub with connection {ConnectionId}",
            UserId, Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Mark user as offline
        await _presenceService.SetUserOfflineAsync(UserId, Context.ConnectionId);
        
        // Only broadcast if user is fully offline (no other connections)
        var isStillOnline = await _presenceService.IsUserOnlineAsync(UserId);
        if (!isStillOnline)
        {
            await Clients.All.SendAsync("UserPresenceChanged", new
            {
                userId = UserId,
                isOnline = false,
                lastSeenUtc = DateTime.UtcNow
            });
        }
        
        _logger.LogInformation("User {UserId} disconnected from chat hub with connection {ConnectionId}",
            UserId, Context.ConnectionId);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Joins a conversation group to receive messages for that conversation.
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        // TODO: Validate user has access to this conversation (check ownership/permissions)
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation(
            "User {UserId} (connection {ConnectionId}) joined conversation {ConversationId}",
            UserId,
            Context.ConnectionId,
            conversationId);
    }

    /// <summary>
    /// Leaves a conversation group to stop receiving messages for that conversation.
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation(
            "User {UserId} (connection {ConnectionId}) left conversation {ConversationId}",
            UserId,
            Context.ConnectionId,
            conversationId);
    }

    /// <summary>
    /// Sends a message to all users in a conversation.
    /// </summary>
    public async Task SendMessage(string conversationId, string content)
    {
        // TODO: Persist message to database via ConversationService
        // TODO: Validate user has access to this conversation
        
        _logger.LogInformation(
            "User {UserId} sent message to conversation {ConversationId}",
            UserId,
            conversationId);
        
        // Broadcast to all clients in the conversation group
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
        {
            UserId,
            ConversationId = conversationId,
            Content = content,
            SentAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcasts typing indicator to other users in the conversation.
    /// </summary>
    public async Task TypingIndicator(string conversationId, bool isTyping)
    {
        await Clients.OthersInGroup(conversationId).SendAsync("UserTyping",
            UserId,
            isTyping);
    }

    /// <summary>
    /// Get online status for a specific user.
    /// </summary>
    public async Task<bool> GetUserOnlineStatus(string userId)
    {
        return await _presenceService.IsUserOnlineAsync(userId);
    }

    /// <summary>
    /// Get all currently online users.
    /// </summary>
    public async Task<IEnumerable<string>> GetOnlineUsers()
    {
        return await _presenceService.GetOnlineUsersAsync();
    }

    /// <summary>
    /// Get last seen timestamp for a user.
    /// </summary>
    public async Task<DateTime?> GetUserLastSeen(string userId)
    {
        return await _presenceService.GetLastSeenAsync(userId);
    }
}
