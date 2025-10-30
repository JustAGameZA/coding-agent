namespace CodingAgent.Services.Dashboard.Infrastructure.Caching;

/// <summary>
/// Interface for caching dashboard data
/// </summary>
public interface IDashboardCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
}
