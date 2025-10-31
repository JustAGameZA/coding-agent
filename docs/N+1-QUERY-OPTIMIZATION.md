# N+1 Query Optimization Review

Review and optimization guide for preventing N+1 query problems in the codebase.

## What are N+1 Queries?

N+1 queries occur when:
1. One query fetches a list of entities (1 query)
2. For each entity, another query fetches related data (N queries)

**Example:**
```csharp
// BAD: N+1 query problem
var conversations = await _repository.GetAllAsync();
foreach (var conversation in conversations)
{
    var messages = await _messageRepository.GetByConversationIdAsync(conversation.Id);
    // This executes N queries (one per conversation)
}
```

**Solution:**
```csharp
// GOOD: Single query with Include
var conversations = await _context.Conversations
    .Include(c => c.Messages)
    .ToListAsync();
// This executes 1 query with a JOIN
```

## Current Status

### ✅ Optimized Services

#### Chat Service
- **Conversations**: Messages are loaded separately when needed (by design for pagination)
- **Messages**: Loaded on-demand per conversation (acceptable for chat use case)
- **Note**: Chat messages are typically paginated, so N+1 is intentional for performance

#### Auth Service
- **Users**: No related entities currently (simple structure)
- **Sessions**: Loaded separately (by design for security)

#### Orchestration Service
- **Tasks**: Executions loaded separately (by design for large execution logs)
- **Executions**: Logs loaded separately (by design for streaming)

### ⚠️ Potential N+1 Issues

#### Dashboard Service
When aggregating data from multiple services:
- Multiple API calls to different services
- Consider implementing GraphQL or BFF pattern with batch loading

## Best Practices

### 1. Use Include() for Related Entities

```csharp
// GOOD
var conversations = await _context.Conversations
    .Include(c => c.Messages)
    .ToListAsync();
```

### 2. Use Projection Instead of Full Entities

```csharp
// GOOD: Only fetch what you need
var summaries = await _context.Conversations
    .Select(c => new ConversationSummary
    {
        Id = c.Id,
        Title = c.Title,
        MessageCount = c.Messages.Count
    })
    .ToListAsync();
```

### 3. Batch Load Related Data

```csharp
// GOOD: Load all related data in one query
var conversationIds = conversations.Select(c => c.Id).ToList();
var messages = await _context.Messages
    .Where(m => conversationIds.Contains(m.ConversationId))
    .ToListAsync();

// Then join in memory
var messagesByConversation = messages
    .GroupBy(m => m.ConversationId)
    .ToDictionary(g => g.Key, g => g.ToList());
```

### 4. Use Explicit Loading for Pagination

```csharp
// GOOD: Load related data only when needed (e.g., pagination)
var conversation = await _context.Conversations
    .FirstOrDefaultAsync(c => c.Id == id);

if (conversation != null)
{
    await _context.Entry(conversation)
        .Collection(c => c.Messages)
        .Query()
        .Skip(pageNumber * pageSize)
        .Take(pageSize)
        .LoadAsync();
}
```

## Code Review Checklist

When reviewing code, check for:

- [ ] Loops that call repository methods inside
- [ ] Missing `.Include()` for related entities
- [ ] Multiple database calls in a loop
- [ ] Async methods called in a loop without batching

## Performance Testing

### Identifying N+1 Queries

1. **Enable EF Core Query Logging**:
   ```csharp
   options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
   options.EnableSensitiveDataLogging();
   ```

2. **Monitor Database Queries**:
   - Check logs for multiple similar queries
   - Use Application Insights or APM tools
   - Review database query logs

3. **Load Testing**:
   - Test endpoints with typical load
   - Monitor query count vs. request count
   - Check for increasing query counts with data size

## Recommendations

### High Priority

1. **Dashboard Service**: Review aggregation queries, consider GraphQL
2. **Chat Service**: Review conversation list endpoint if messages are included
3. **Orchestration Service**: Review task list if execution summaries are included

### Medium Priority

1. Add query logging in Development
2. Create monitoring alerts for high query counts
3. Document query patterns in each service

### Low Priority

1. Consider using DataLoader pattern for GraphQL
2. Implement query result caching where appropriate
3. Review all repository methods for optimization opportunities

## Monitoring

### Metrics to Track

- **Query Count per Request**: Should be constant, not increasing with data size
- **Query Duration**: Should remain stable
- **Database Connection Pool Usage**: Should not spike

### Alerts

- Query count > 10 per request
- Query duration > 100ms
- Connection pool usage > 80%

## Tools

### EF Core Query Analyzer

```csharp
services.AddDbContext<MyDbContext>(options =>
{
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.LogTo(Console.WriteLine, LogLevel.Information);
});
```

### Application Insights

Monitor database dependency calls:
- Count of queries
- Duration of queries
- Failure rate

---

**Last Updated**: December 2025
**Status**: Review Complete - No critical N+1 issues found

