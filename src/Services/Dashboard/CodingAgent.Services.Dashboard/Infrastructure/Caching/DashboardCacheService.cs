using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CodingAgent.Services.Dashboard.Infrastructure.Caching;

/// <summary>
/// Caching service for dashboard data
/// </summary>
public class DashboardCacheService : IDashboardCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DashboardCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public DashboardCacheService(IDistributedCache cache, ILogger<DashboardCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var cachedBytes = await _cache.GetAsync(key, ct);
            if (cachedBytes == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var cached = Encoding.UTF8.GetString(cachedBytes);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cached value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(json);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            await _cache.SetAsync(key, bytes, options, ct);
            _logger.LogDebug("Cached value for key: {Key} with expiration {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogDebug("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache for key: {Key}", key);
        }
    }
}
