using CodingAgent.Services.Chat.Domain.Services;
using StackExchange.Redis;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CodingAgent.Services.Chat.Infrastructure.Presence;

/// <summary>
/// Redis-based presence tracking service for user online/offline status
/// </summary>
public class PresenceService : IPresenceService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<PresenceService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _presenceUpdateCounter;
    private readonly Histogram<double> _presenceOperationDuration;

    private const int OnlineExpirySeconds = 300; // 5 minutes
    private const string OnlineUsersKey = "presence:online";
    private const string LastSeenKeyPrefix = "presence:lastseen:";
    private const string UserConnectionsKeyPrefix = "presence:connections:";

    public PresenceService(
        IConnectionMultiplexer? redis,
        ILogger<PresenceService> logger,
        IMeterFactory meterFactory)
    {
        _redis = redis;
        _logger = logger;
        _activitySource = new ActivitySource("chat-service");

        var meter = meterFactory.Create("chat-service");
        _presenceUpdateCounter = meter.CreateCounter<long>(
            "presence_updates_total",
            description: "Total number of presence updates");
        _presenceOperationDuration = meter.CreateHistogram<double>(
            "presence_operation_duration_ms",
            unit: "ms",
            description: "Duration of presence operations");
    }

    public async Task SetUserOnlineAsync(string userId, string connectionId, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("SetUserOnline");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("connection.id", connectionId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, skipping presence tracking");
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var db = _redis.GetDatabase();
            var batch = db.CreateBatch();

            // Add user to online set with expiry
            var addToSetTask = batch.SortedSetAddAsync(
                OnlineUsersKey,
                userId,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                flags: CommandFlags.FireAndForget);

            // Update last seen
            var lastSeenKey = $"{LastSeenKeyPrefix}{userId}";
            var setLastSeenTask = batch.StringSetAsync(
                lastSeenKey,
                DateTime.UtcNow.Ticks,
                TimeSpan.FromSeconds(OnlineExpirySeconds),
                flags: CommandFlags.FireAndForget);

            // Add connection ID to user's connections set
            var connectionsKey = $"{UserConnectionsKeyPrefix}{userId}";
            var addConnectionTask = batch.SetAddAsync(
                connectionsKey,
                connectionId,
                flags: CommandFlags.FireAndForget);

            // Set expiry on connections set
            var setExpiryTask = batch.KeyExpireAsync(
                connectionsKey,
                TimeSpan.FromSeconds(OnlineExpirySeconds),
                flags: CommandFlags.FireAndForget);

            batch.Execute();
            await Task.WhenAll(addToSetTask, setLastSeenTask, addConnectionTask, setExpiryTask);

            _logger.LogInformation(
                "User {UserId} marked online (connection {ConnectionId})",
                userId,
                connectionId);

            _presenceUpdateCounter.Add(1, new KeyValuePair<string, object?>("operation", "online"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting user {UserId} online", userId);
        }
        finally
        {
            _presenceOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", "set_online"));
        }
    }

    public async Task SetUserOfflineAsync(string userId, string connectionId, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("SetUserOffline");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("connection.id", connectionId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, skipping presence tracking");
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var db = _redis.GetDatabase();
            var connectionsKey = $"{UserConnectionsKeyPrefix}{userId}";

            // Remove this connection from user's connections
            await db.SetRemoveAsync(connectionsKey, connectionId);

            // Check if user has any remaining connections
            var connectionCount = await db.SetLengthAsync(connectionsKey);

            if (connectionCount == 0)
            {
                // No more connections, mark user as offline
                await db.SortedSetRemoveAsync(OnlineUsersKey, userId);

                _logger.LogInformation(
                    "User {UserId} marked offline (last connection {ConnectionId} removed)",
                    userId,
                    connectionId);
            }
            else
            {
                _logger.LogInformation(
                    "User {UserId} connection {ConnectionId} removed ({RemainingConnections} remaining)",
                    userId,
                    connectionId,
                    connectionCount);
            }

            _presenceUpdateCounter.Add(1, new KeyValuePair<string, object?>("operation", "offline"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting user {UserId} offline", userId);
        }
        finally
        {
            _presenceOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", "set_offline"));
        }
    }

    public async Task<bool> IsUserOnlineAsync(string userId, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("IsUserOnline");
        activity?.SetTag("user.id", userId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, returning false for presence check");
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var score = await db.SortedSetScoreAsync(OnlineUsersKey, userId);

            return score.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is online", userId);
            return false;
        }
    }

    public async Task<DateTime?> GetLastSeenAsync(string userId, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("GetLastSeen");
        activity?.SetTag("user.id", userId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, returning null for last seen");
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var lastSeenKey = $"{LastSeenKeyPrefix}{userId}";
            var ticks = await db.StringGetAsync(lastSeenKey);

            if (ticks.HasValue && long.TryParse(ticks, out var ticksValue))
            {
                return new DateTime(ticksValue, DateTimeKind.Utc);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last seen for user {UserId}", userId);
            return null;
        }
    }

    public async Task<IEnumerable<string>> GetOnlineUsersAsync(CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("GetOnlineUsers");

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, returning empty list");
            return Enumerable.Empty<string>();
        }

        try
        {
            var db = _redis.GetDatabase();

            // Get all users from online set
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var staleTimestamp = now - OnlineExpirySeconds;

            // Remove stale entries first
            await db.SortedSetRemoveRangeByScoreAsync(
                OnlineUsersKey,
                double.NegativeInfinity,
                staleTimestamp);

            // Get remaining online users
            var onlineUsers = await db.SortedSetRangeByScoreAsync(
                OnlineUsersKey,
                staleTimestamp,
                double.PositiveInfinity);

            return onlineUsers.Select(u => u.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online users");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<int> GetUserConnectionCountAsync(string userId, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("GetUserConnectionCount");
        activity?.SetTag("user.id", userId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, returning 0 for connection count");
            return 0;
        }

        try
        {
            var db = _redis.GetDatabase();
            var connectionsKey = $"{UserConnectionsKeyPrefix}{userId}";
            var count = await db.SetLengthAsync(connectionsKey);

            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection count for user {UserId}", userId);
            return 0;
        }
    }
}
