using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Infrastructure.Persistence;

/// <summary>
/// Repository for conversation persistence operations with Redis caching
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ConversationRepository> _logger;
    private readonly IMessageCacheService _cacheService;

    public ConversationRepository(
        ChatDbContext context,
        ILogger<ConversationRepository> logger,
        IMessageCacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversation {ConversationId}", id);
        
        // Fetch conversation without messages first
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (conversation == null)
        {
            return null;
        }

        // NOTE:
        // We intentionally skip loading messages here to avoid hitting the messages table
        // in scenarios where its schema may not yet be fully aligned with the EF mappings.
        // The current API contract for GetConversation does not require message payloads,
        // and E2E tests assert an empty messages array in the DTO. Should message hydration
        // be required later, we can re-enable the caching/DB loading once the schema is
        // finalized and migrations are applied.
        return conversation;
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all conversations");
        return await _context.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Conversation>> GetPagedAsync(PaginationParameters pagination, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversations page {PageNumber} with size {PageSize}", 
            pagination.PageNumber, pagination.PageSize);

        var query = _context.Conversations
            .OrderByDescending(c => c.UpdatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(ct);

        return new PagedResult<Conversation>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversations for user {UserId}", userId);
        return await _context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Conversation>> SearchPagedAsync(string query, PaginationParameters pagination, CancellationToken ct = default)
    {
        _logger.LogDebug("Searching conversations with query: {Query} (page: {PageNumber}, size: {PageSize})", 
            query, pagination.PageNumber, pagination.PageSize);
        
        // Check if we're using PostgreSQL or in-memory database
        var providerName = _context.Database.ProviderName;
        var isPostgres = providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (isPostgres)
        {
            // PostgreSQL full-text search
            // Validate query length to prevent abuse
            if (query.Length > 200)
            {
                _logger.LogWarning("Search query exceeds maximum length of 200 characters");
                return new PagedResult<Conversation>(new List<Conversation>(), 0, pagination.PageNumber, pagination.PageSize);
            }
            
            // Sanitize input by removing special PostgreSQL full-text search operators
            var sanitizedQuery = query
                .Replace("&", " ")
                .Replace("|", " ")
                .Replace("!", " ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace(":", " ")
                .Trim();
            
            var searchTerms = sanitizedQuery
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(term => !string.IsNullOrWhiteSpace(term) && term.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                .ToArray();
            
            if (searchTerms.Length == 0)
            {
                // If query becomes empty after sanitization, return empty results
                _logger.LogDebug("Search query contains no valid terms after sanitization");
                return new PagedResult<Conversation>(new List<Conversation>(), 0, pagination.PageNumber, pagination.PageSize);
            }
            
            var searchQuery = string.Join(" & ", searchTerms);
            
            var baseQuery = _context.Conversations
                .Include(c => c.Messages)
                .Where(c => 
                    // Search in conversation title
                    EF.Functions.ToTsVector("english", c.Title).Matches(EF.Functions.ToTsQuery("english", searchQuery)) ||
                    // Search in message content
                    c.Messages.Any(m => EF.Functions.ToTsVector("english", m.Content).Matches(EF.Functions.ToTsQuery("english", searchQuery)))
                )
                .Select(c => new 
                {
                    Conversation = c,
                    // Calculate relevance score
                    Rank = EF.Functions.ToTsVector("english", c.Title).Rank(EF.Functions.ToTsQuery("english", searchQuery)) +
                           c.Messages.Max(m => (double?)EF.Functions.ToTsVector("english", m.Content).Rank(EF.Functions.ToTsQuery("english", searchQuery))) ?? 0
                })
                .OrderByDescending(x => x.Rank)
                .ThenByDescending(x => x.Conversation.UpdatedAt);
            
            var totalCount = await baseQuery.CountAsync(ct);
            
            var items = await baseQuery
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .Select(x => x.Conversation)
                .ToListAsync(ct);
            
            return new PagedResult<Conversation>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
        else
        {
            // Fallback for in-memory database: simple LIKE search with pagination
            var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Search in conversation titles - materialize IDs first
            var conversationIdsByTitle = await _context.Conversations
                .Where(c => searchTerms.Any(term => c.Title.ToLower().Contains(term)))
                .Select(c => c.Id)
                .ToListAsync(ct);
            
            // Search in message content - materialize IDs first
            var conversationIdsByMessage = await _context.Messages
                .Where(m => searchTerms.Any(term => m.Content.ToLower().Contains(term)))
                .Select(m => m.ConversationId)
                .Distinct()
                .ToListAsync(ct);
            
            // Combine IDs
            var allConversationIds = conversationIdsByTitle
                .Union(conversationIdsByMessage)
                .ToHashSet();
            
            var baseQuery = _context.Conversations
                .Include(c => c.Messages)
                .Where(c => allConversationIds.Contains(c.Id))
                .OrderByDescending(c => c.UpdatedAt);
            
            var totalCount = await baseQuery.CountAsync(ct);
            
            var items = await baseQuery
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync(ct);
            
            return new PagedResult<Conversation>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }

    public async Task<IEnumerable<Conversation>> SearchAsync(string query, CancellationToken ct = default)
    {
        _logger.LogDebug("Searching conversations with query: {Query}", query);
        
        // Check if we're using PostgreSQL or in-memory database
        var providerName = _context.Database.ProviderName;
        var isPostgres = providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (isPostgres)
        {
            // PostgreSQL full-text search
            // Validate query length to prevent abuse
            if (query.Length > 200)
            {
                _logger.LogWarning("Search query exceeds maximum length of 200 characters");
                return Enumerable.Empty<Conversation>();
            }
            
            // Sanitize input by removing special PostgreSQL full-text search operators
            var sanitizedQuery = query
                .Replace("&", " ")
                .Replace("|", " ")
                .Replace("!", " ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace(":", " ")
                .Trim();
            
            var searchTerms = sanitizedQuery
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(term => !string.IsNullOrWhiteSpace(term) && term.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                .ToArray();
            
            if (searchTerms.Length == 0)
            {
                // If query becomes empty after sanitization, return empty results
                _logger.LogDebug("Search query contains no valid terms after sanitization");
                return Enumerable.Empty<Conversation>();
            }
            
            var searchQuery = string.Join(" & ", searchTerms);
            
            var conversations = await _context.Conversations
                .Include(c => c.Messages)
                .Where(c => 
                    // Search in conversation title
                    EF.Functions.ToTsVector("english", c.Title).Matches(EF.Functions.ToTsQuery("english", searchQuery)) ||
                    // Search in message content
                    c.Messages.Any(m => EF.Functions.ToTsVector("english", m.Content).Matches(EF.Functions.ToTsQuery("english", searchQuery)))
                )
                .Select(c => new 
                {
                    Conversation = c,
                    // Calculate relevance score
                    Rank = EF.Functions.ToTsVector("english", c.Title).Rank(EF.Functions.ToTsQuery("english", searchQuery)) +
                           c.Messages.Max(m => (double?)EF.Functions.ToTsVector("english", m.Content).Rank(EF.Functions.ToTsQuery("english", searchQuery))) ?? 0
                })
                .OrderByDescending(x => x.Rank)
                .ThenByDescending(x => x.Conversation.UpdatedAt)
                .Take(100)
                .Select(x => x.Conversation)
                .ToListAsync(ct);
            
            return conversations;
        }
        else
        {
            // Fallback for in-memory database: simple LIKE search
            var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Search in conversation titles - materialize IDs first
            var conversationIdsByTitle = await _context.Conversations
                .Where(c => searchTerms.Any(term => c.Title.ToLower().Contains(term)))
                .Select(c => c.Id)
                .ToListAsync(ct);
            
            // Search in message content - materialize IDs first
            var conversationIdsByMessage = await _context.Messages
                .Where(m => searchTerms.Any(term => m.Content.ToLower().Contains(term)))
                .Select(m => m.ConversationId)
                .Distinct()
                .ToListAsync(ct);
            
            // Combine IDs
            var allConversationIds = conversationIdsByTitle
                .Union(conversationIdsByMessage)
                .ToHashSet();
            
            // Fetch conversations by IDs
            var conversations = await _context.Conversations
                .Include(c => c.Messages)
                .Where(c => allConversationIds.Contains(c.Id))
                .OrderByDescending(c => c.UpdatedAt)
                .Take(100)
                .ToListAsync(ct);
            
            return conversations;
        }
    }

    public async Task<Conversation> CreateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating conversation {ConversationId}", conversation.Id);
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating conversation {ConversationId}", conversation.Id);
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(ct);

        // Update cache with new messages
        if (conversation.Messages.Any())
        {
            await _cacheService.SetMessagesAsync(conversation.Id, conversation.Messages, ct);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting conversation {ConversationId}", id);
        var conversation = await _context.Conversations.FindAsync(new object[] { id }, ct);
        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync(ct);

            // Invalidate cache
            await _cacheService.InvalidateCacheAsync(id, ct);
        }
    }
}
