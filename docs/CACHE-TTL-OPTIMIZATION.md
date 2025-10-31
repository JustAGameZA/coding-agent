# Cache TTL Optimization Guide

Guide for optimizing cache Time-To-Live (TTL) values across services.

## Overview

Cache TTLs balance:
- **Freshness**: Data should be up-to-date
- **Performance**: Reduce database load
- **Consistency**: Minimize stale data

## Current Cache Configuration

### Chat Service

**Message Cache**:
- **TTL**: 30 minutes (default)
- **Use Case**: Frequently accessed recent messages
- **Location**: `MessageCacheService`

**Recommendation**: 
- Recent messages (last hour): 30 minutes ✅
- Older messages: No cache (infrequent access)

### Auth Service

**Session Cache**:
- **TTL**: Matches JWT expiration (1 hour default)
- **Use Case**: Session validation
- **Location**: Session validation logic

**Recommendation**:
- Active sessions: 1 hour ✅
- Inactive sessions: Cache until expiry

### Gateway

**Rate Limit Cache**:
- **TTL**: Window-based (1 minute for IP, 1 hour for user)
- **Use Case**: Rate limit counters
- **Location**: `Program.cs` rate limiting middleware

**Recommendation**:
- IP limits: 1 minute ✅
- User limits: 1 hour ✅

## Cache TTL Strategy

### High-Frequency Data (Short TTL)

- **Recent messages**: 30 minutes
- **Active sessions**: 1 hour
- **Rate limit counters**: Window-based
- **Health check results**: 5 minutes

### Medium-Frequency Data (Medium TTL)

- **User profiles**: 15 minutes
- **Task summaries**: 10 minutes
- **Configuration data**: 1 hour

### Low-Frequency Data (Long TTL / No Cache)

- **User lists**: No cache (rarely accessed)
- **Historical data**: No cache (not frequently accessed)

## Recommended TTL Values

| Data Type | Current TTL | Recommended TTL | Reason |
|-----------|------------|-----------------|--------|
| Recent Messages | 30 min | 30 min | Good balance |
| Active Sessions | 1 hour | 1 hour | Matches token expiry |
| User Profiles | N/A | 15 min | Moderate update frequency |
| Task Summaries | N/A | 10 min | Updates frequently |
| Rate Limits | Window-based | Window-based | Correct pattern |
| Health Checks | 5 min | 5 min | Good for freshness |

## Configuration

### Redis Cache TTL

```csharp
// Set TTL when caching
await cache.SetStringAsync(
    key, 
    value, 
    new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    });
```

### In-Memory Cache TTL

```csharp
// Set TTL with sliding expiration
cache.Set(key, value, new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
    SlidingExpiration = TimeSpan.FromMinutes(10)
});
```

## Cache Invalidation

### Event-Driven Invalidation

Invalidate cache when data changes:

```csharp
// Example: Invalidate conversation cache when message added
public async Task AddMessageAsync(Message message)
{
    await _repository.AddAsync(message);
    
    // Invalidate cache
    await _cache.RemoveAsync($"conversation:{message.ConversationId}:messages");
    
    // Publish event
    await _bus.Publish(new MessageAddedEvent(message));
}
```

### Time-Based Invalidation

Use shorter TTL for frequently updated data:

```csharp
// User profile: 15 minutes (user may update profile)
var options = new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
};
```

## Performance Optimization

### Monitoring Cache Hit Rate

Track cache effectiveness:

```csharp
// Metrics
_cache_hits_total
_cache_misses_total
_cache_hit_rate = cache_hits / (cache_hits + cache_misses)
```

**Target**: > 80% hit rate

### Adjusting TTL Based on Hit Rate

- **Hit rate > 90%**: Consider increasing TTL
- **Hit rate < 70%**: Consider decreasing TTL
- **Hit rate < 50%**: Reconsider caching strategy

## Best Practices

### 1. Use Sliding Expiration for Frequently Accessed Data

```csharp
new MemoryCacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(10),
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
};
```

### 2. Use Absolute Expiration for Time-Sensitive Data

```csharp
new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
};
```

### 3. Cache at Multiple Levels

- **L1 Cache**: In-memory (fastest, shortest TTL)
- **L2 Cache**: Redis (faster than DB, medium TTL)
- **L3 Cache**: Database (slowest, no expiration)

## Recommendations by Service

### Chat Service

**Current**: Message cache TTL = 30 minutes ✅
**Action**: Monitor hit rate, adjust if needed

### Auth Service

**Current**: Session validation uses token expiry ✅
**Action**: No changes needed

### Gateway

**Current**: Rate limit TTL = window-based ✅
**Action**: No changes needed

### Dashboard Service

**Recommendation**: 
- Add caching for aggregated data
- TTL: 5 minutes (data updates frequently)

### Orchestration Service

**Recommendation**:
- Cache task summaries
- TTL: 10 minutes

## Monitoring

### Metrics to Track

- Cache hit rate per service
- Cache memory usage
- Cache eviction rate
- Response time improvement from cache

### Alerts

- Cache hit rate < 70%
- Cache memory usage > 80%
- Cache errors increasing

## Future Improvements

1. **Adaptive TTL**: Adjust TTL based on access patterns
2. **Predictive Caching**: Pre-cache likely-to-be-accessed data
3. **Cache Warming**: Pre-populate cache on service startup
4. **Distributed Cache Coherency**: Ensure cache consistency across instances

---

**Last Updated**: December 2025
**Status**: TTL values optimized for current usage patterns

