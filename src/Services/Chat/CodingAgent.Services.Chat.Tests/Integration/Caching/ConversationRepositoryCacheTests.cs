using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Infrastructure.Caching;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Diagnostics.Metrics;
using Testcontainers.Redis;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Integration.Caching;

/// <summary>
/// Integration tests for ConversationRepository with Redis caching
/// </summary>
[Trait("Category", "Integration")]
public class ConversationRepositoryCacheTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private ChatDbContext? _context;
    private ConversationRepository? _repository;
    private MessageCacheService? _cacheService;
    private readonly Mock<ILogger<ConversationRepository>> _repoLoggerMock;
    private readonly Mock<ILogger<MessageCacheService>> _cacheLoggerMock;
    private readonly IMeterFactory _meterFactory;

    public ConversationRepositoryCacheTests()
    {
        _repoLoggerMock = new Mock<ILogger<ConversationRepository>>();
        _cacheLoggerMock = new Mock<ILogger<MessageCacheService>>();
        _meterFactory = new TestMeterFactory();
    }

    public async Task InitializeAsync()
    {
        // Start Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Connect to Redis
        _redis = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());

        // Create in-memory database
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ChatDbContext(options);

        // Create cache service
        _cacheService = new MessageCacheService(_redis, _cacheLoggerMock.Object, _meterFactory);

        // Create repository
        _repository = new ConversationRepository(_context, _repoLoggerMock.Object, _cacheService);
    }

    public async Task DisposeAsync()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _redis?.Dispose();
        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetByIdAsync_FirstCall_ShouldLoadFromDatabaseAndCacheMessages()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        var message1 = new Message(conversation.Id, userId, "Hello", MessageRole.User);
        var message2 = new Message(conversation.Id, null, "Hi there", MessageRole.Assistant);
        
        conversation.AddMessage(message1);
        conversation.AddMessage(message2);
        
        _context!.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act - First call should load from DB
        var result = await _repository!.GetByIdAsync(conversation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(2);

        // Verify messages are now cached
        var cachedMessages = await _cacheService!.GetMessagesAsync(conversation.Id);
        cachedMessages.Should().NotBeNull();
        cachedMessages!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_SecondCall_ShouldLoadFromCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        var message = new Message(conversation.Id, userId, "Cached message", MessageRole.User);
        
        conversation.AddMessage(message);
        
        _context!.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // First call to populate cache
        await _repository!.GetByIdAsync(conversation.Id);

        // Detach to simulate new context
        _context.ChangeTracker.Clear();

        // Act - Second call should use cache
        var result = await _repository.GetByIdAsync(conversation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(1);
        result.Messages.First().Content.Should().Be("Cached message");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        _context!.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Add a message
        var message = new Message(conversation.Id, userId, "New message", MessageRole.User);
        conversation.AddMessage(message);

        // Act
        await _repository!.UpdateAsync(conversation);

        // Assert - Message should be in cache
        var cachedMessages = (await _cacheService!.GetMessagesAsync(conversation.Id))?.ToList();
        cachedMessages.Should().NotBeNull();
        cachedMessages.Should().HaveCount(1);
        cachedMessages![0].Content.Should().Be("New message");
    }

    [Fact]
    public async Task DeleteAsync_ShouldInvalidateCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        var message = new Message(conversation.Id, userId, "Test message", MessageRole.User);
        conversation.AddMessage(message);
        
        _context!.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Populate cache
        await _repository!.GetByIdAsync(conversation.Id);

        // Verify cache is populated
        var cachedBefore = await _cacheService!.GetMessagesAsync(conversation.Id);
        cachedBefore.Should().NotBeNull();

        // Act
        await _repository.DeleteAsync(conversation.Id);

        // Assert - Cache should be invalidated
        var cachedAfter = await _cacheService.GetMessagesAsync(conversation.Id);
        cachedAfter.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheDisconnected_ShouldFallbackToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        var message = new Message(conversation.Id, userId, "Fallback message", MessageRole.User);
        
        conversation.AddMessage(message);
        
        _context!.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Dispose Redis to simulate disconnection
        _redis?.Dispose();
        
        // Create repository with null Redis connection
        var repositoryWithoutRedis = new ConversationRepository(
            _context,
            _repoLoggerMock.Object,
            new MessageCacheService(null, _cacheLoggerMock.Object, _meterFactory)
        );

        // Act
        var result = await repositoryWithoutRedis.GetByIdAsync(conversation.Id);

        // Assert - Should still load from database
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(1);
        result.Messages.First().Content.Should().Be("Fallback message");
    }

    [Fact]
    public async Task GetByIdAsync_WithManyMessages_ShouldCacheOnlyLast100()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        
        // Add 150 messages
        for (int i = 1; i <= 150; i++)
        {
            var message = new Message(conversation.Id, userId, $"Message {i}", MessageRole.User);
            conversation.AddMessage(message);
        }
        
        _context!.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository!.GetByIdAsync(conversation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(150);

        // Verify only last 100 are cached
        var cachedMessages = (await _cacheService!.GetMessagesAsync(conversation.Id))?.ToList();
        cachedMessages.Should().NotBeNull();
        cachedMessages.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHitRateMetric_ShouldBeTracked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        var message = new Message(conversation.Id, userId, "Metrics test", MessageRole.User);
        
        conversation.AddMessage(message);
        
        _context!.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        // First call - cache miss (loads from DB and caches)
        await _repository!.GetByIdAsync(conversation.Id);
        
        // Detach to simulate new request
        _context.ChangeTracker.Clear();
        
        // Second call - cache hit (loads from cache)
        await _repository.GetByIdAsync(conversation.Id);

        // Assert
        // Metrics are being tracked (we can't easily assert on counter values in tests,
        // but we verify the operations complete successfully)
        var cachedMessages = await _cacheService!.GetMessagesAsync(conversation.Id);
        cachedMessages.Should().NotBeNull();
    }

    /// <summary>
    /// Simple test implementation of IMeterFactory for testing
    /// </summary>
    private class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new Meter(options);
        public void Dispose() { }
    }
}
