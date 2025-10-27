namespace CodingAgent.Services.Chat.Domain.Services;

/// <summary>
/// Service for tracking user presence (online/offline status)
/// </summary>
public interface IPresenceService
{
    /// <summary>
    /// Mark a user as online
    /// </summary>
    Task SetUserOnlineAsync(string userId, string connectionId, CancellationToken ct = default);

    /// <summary>
    /// Mark a user as offline
    /// </summary>
    Task SetUserOfflineAsync(string userId, string connectionId, CancellationToken ct = default);

    /// <summary>
    /// Get online status for a user
    /// </summary>
    Task<bool> IsUserOnlineAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get last seen timestamp for a user
    /// </summary>
    Task<DateTime?> GetLastSeenAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get all online users
    /// </summary>
    Task<IEnumerable<string>> GetOnlineUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Get connection count for a user (supports multiple connections)
    /// </summary>
    Task<int> GetUserConnectionCountAsync(string userId, CancellationToken ct = default);
}
