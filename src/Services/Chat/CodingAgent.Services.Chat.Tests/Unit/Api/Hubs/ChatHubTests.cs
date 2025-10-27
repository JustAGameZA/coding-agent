using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Domain.Services;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Api.Hubs;

[Trait("Category", "Unit")]
public class ChatHubTests
{
    private readonly TestLogger<ChatHub> _testLogger;
    private readonly Mock<IPresenceService> _presenceServiceMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly ChatHub _chatHub;
    private readonly ClaimsPrincipal _testUser;
    private const string TestUserId = "test-user-123";
    private const string TestConnectionId = "test-connection-id";

    public ChatHubTests()
    {
        _testLogger = new TestLogger<ChatHub>();
        _presenceServiceMock = new Mock<IPresenceService>();
        _contextMock = new Mock<HubCallerContext>();
        _clientsMock = new Mock<IHubCallerClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _groupManagerMock = new Mock<IGroupManager>();

        // Setup test user with ClaimsPrincipal
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        };
        _testUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Setup context
        _contextMock.Setup(c => c.User).Returns(_testUser);
        _contextMock.Setup(c => c.ConnectionId).Returns(TestConnectionId);

        // Setup clients
        _clientsMock.Setup(c => c.Others).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        // Create ChatHub instance
        _chatHub = new ChatHub(_testLogger, _presenceServiceMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object,
            Groups = _groupManagerMock.Object
        };
    }

    // Simple test logger to capture log entries without Moq expression trees
    private sealed class TestLogger<T> : ILogger<T>
    {
        public readonly List<LogRecord> Records = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter != null ? formatter(state, exception) : state?.ToString() ?? string.Empty;
            Records.Add(new LogRecord(logLevel, eventId, state?.ToString() ?? string.Empty, exception, message));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }

    private sealed record LogRecord(LogLevel LogLevel, EventId EventId, string State, Exception? Exception, string Message);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var hub = new ChatHub(_testLogger, _presenceServiceMock.Object);

        // Assert
        hub.Should().NotBeNull();
    }

    #endregion

    #region OnConnectedAsync Tests

    [Fact]
    public async Task OnConnectedAsync_ShouldLogConnection()
    {
        // Act
        await _chatHub.OnConnectedAsync();

        // Assert
        var infoLogs = _testLogger.Records.Where(r => r.LogLevel == LogLevel.Information).ToList();
        infoLogs.Should().NotBeEmpty();
        infoLogs.Any(r => r.Message.Contains(TestUserId) && r.Message.Contains(TestConnectionId) && r.Message.Contains("connected", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldBroadcastPresenceOnline()
    {
        // Arrange
        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.OnConnectedAsync();

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "UserPresenceChanged",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldSendCorrectPresenceData()
    {
        // Arrange
        object? capturedData = null;
        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                "UserPresenceChanged",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, ct) =>
            {
                capturedData = args[0];
            })
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.OnConnectedAsync();

        // Assert
        capturedData.Should().NotBeNull();
        var data = capturedData!.GetType().GetProperty("userId")?.GetValue(capturedData);
        data.Should().Be(TestUserId);
        
        var isOnline = capturedData.GetType().GetProperty("isOnline")?.GetValue(capturedData);
        isOnline.Should().Be(true);
    }

    #endregion

    #region OnDisconnectedAsync Tests

    [Fact]
    public async Task OnDisconnectedAsync_WithoutException_ShouldLogDisconnection()
    {
        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert
        var infoLogs = _testLogger.Records.Where(r => r.LogLevel == LogLevel.Information).ToList();
        infoLogs.Should().NotBeEmpty();
        infoLogs.Any(r => r.Message.Contains(TestUserId) && r.Message.Contains(TestConnectionId) && r.Message.Contains("disconnected", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_ShouldLogInformation()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        await _chatHub.OnDisconnectedAsync(exception);

        // Assert
        var infoLogs = _testLogger.Records.Where(r => r.LogLevel == LogLevel.Information).ToList();
        infoLogs.Should().NotBeEmpty();
        infoLogs.Any(r => r.Message.Contains(TestUserId) && r.Message.Contains("disconnected"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldUpdatePresence()
    {
        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert
        _presenceServiceMock.Verify(
            x => x.SetUserOfflineAsync(TestUserId, TestConnectionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldCheckIfUserIsStillOnline()
    {
        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert
        _presenceServiceMock.Verify(
            x => x.IsUserOnlineAsync(TestUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenUserOffline_ShouldBroadcastPresenceOffline()
    {
        // Arrange
        _presenceServiceMock
            .Setup(x => x.IsUserOnlineAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "UserPresenceChanged",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenUserStillOnline_ShouldNotNotifyOthers()
    {
        // Arrange
        _presenceServiceMock
            .Setup(x => x.IsUserOnlineAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "UserPresenceChanged",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenUserOffline_ShouldSendCorrectPresenceData()
    {
        // Arrange
        _presenceServiceMock
            .Setup(x => x.IsUserOnlineAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        object? capturedData = null;
        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                "UserPresenceChanged",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, ct) =>
            {
                capturedData = args[0];
            })
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert
        capturedData.Should().NotBeNull();
        var data = capturedData!.GetType().GetProperty("userId")?.GetValue(capturedData);
        data.Should().Be(TestUserId);
        
        var isOnline = capturedData.GetType().GetProperty("isOnline")?.GetValue(capturedData);
        isOnline.Should().Be(false);
    }

    #endregion

    #region JoinConversation Tests

    [Fact]
    public async Task JoinConversation_ShouldAddToGroup()
    {
        // Arrange
        var conversationId = "conversation-123";

        // Act
        await _chatHub.JoinConversation(conversationId);

        // Assert
        _groupManagerMock.Verify(
            x => x.AddToGroupAsync(TestConnectionId, conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinConversation_ShouldLogAction()
    {
        // Arrange
        var conversationId = "conversation-123";

        // Act
        await _chatHub.JoinConversation(conversationId);

        // Assert
        var infoLogs = _testLogger.Records.Where(r => r.LogLevel == LogLevel.Information).ToList();
        infoLogs.Any(r => r.Message.Contains(TestUserId)
                       && r.Message.Contains(TestConnectionId)
                       && r.Message.Contains(conversationId)
                       && r.Message.Contains("joined", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    #endregion

    #region LeaveConversation Tests

    [Fact]
    public async Task LeaveConversation_ShouldRemoveFromGroup()
    {
        // Arrange
        var conversationId = "conversation-123";

        // Act
        await _chatHub.LeaveConversation(conversationId);

        // Assert
        _groupManagerMock.Verify(
            x => x.RemoveFromGroupAsync(TestConnectionId, conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveConversation_ShouldLogAction()
    {
        // Arrange
        var conversationId = "conversation-123";

        // Act
        await _chatHub.LeaveConversation(conversationId);

        // Assert
        var infoLogs = _testLogger.Records.Where(r => r.LogLevel == LogLevel.Information).ToList();
        infoLogs.Any(r => r.Message.Contains(TestUserId)
                       && r.Message.Contains(TestConnectionId)
                       && r.Message.Contains(conversationId)
                       && r.Message.Contains("left", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    #endregion

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_ShouldLogAction()
    {
        // Arrange
        var conversationId = "conversation-123";
        var content = "Hello, world!";

        // Act
        await _chatHub.SendMessage(conversationId, content);

        // Assert
        var infoLogs = _testLogger.Records.Where(r => r.LogLevel == LogLevel.Information).ToList();
        infoLogs.Any(r => r.Message.Contains(TestUserId)
                       && r.Message.Contains(conversationId)
                       && r.Message.Contains("sent message", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_ShouldBroadcastToGroup()
    {
        // Arrange
        var conversationId = "conversation-123";
        var content = "Hello, world!";

        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.SendMessage(conversationId, content);

        // Assert
        _clientsMock.Verify(
            x => x.Group(conversationId),
            Times.Once);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_ShouldSendCorrectMessageData()
    {
        // Arrange
        var conversationId = "conversation-123";
        var content = "Hello, world!";
        object? capturedData = null;

        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                "ReceiveMessage",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, ct) =>
            {
                capturedData = args[0];
            })
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.SendMessage(conversationId, content);

        // Assert
        capturedData.Should().NotBeNull();
        
        var userId = capturedData!.GetType().GetProperty("UserId")?.GetValue(capturedData);
        userId.Should().Be(TestUserId);
        
        var convId = capturedData.GetType().GetProperty("ConversationId")?.GetValue(capturedData);
        convId.Should().Be(conversationId);
        
        var messageContent = capturedData.GetType().GetProperty("Content")?.GetValue(capturedData);
        messageContent.Should().Be(content);
        
        var sentAt = capturedData.GetType().GetProperty("SentAt")?.GetValue(capturedData);
        sentAt.Should().NotBeNull();
    }

    #endregion

    #region TypingIndicator Tests

    [Fact]
    public async Task TypingIndicator_ShouldNotifyOthersInGroup()
    {
        // Arrange
        var conversationId = "conversation-123";
        var isTyping = true;

        string? capturedMethod = null;
        object?[]? capturedArgs = null;
        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((m, a, ct) => { capturedMethod = m; capturedArgs = a; })
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.TypingIndicator(conversationId, isTyping);

        // Assert
        _clientsMock.Verify(
            x => x.OthersInGroup(conversationId),
            Times.Once);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "UserTyping",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        capturedMethod.Should().Be("UserTyping");
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Length.Should().Be(2);
        capturedArgs[0]!.ToString().Should().Be(TestUserId);
        ((bool)capturedArgs[1]!).Should().Be(isTyping);
    }

    [Fact]
    public async Task TypingIndicator_WithFalse_ShouldSendCorrectStatus()
    {
        // Arrange
        var conversationId = "conversation-123";
        var isTyping = false;

        object?[]? capturedArgs = null;
        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((m, a, ct) => { capturedArgs = a; })
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.TypingIndicator(conversationId, isTyping);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "UserTyping",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        capturedArgs.Should().NotBeNull();
        capturedArgs!.Length.Should().Be(2);
        capturedArgs[0]!.ToString().Should().Be(TestUserId);
        ((bool)capturedArgs[1]!).Should().BeFalse();
    }

    #endregion

    #region GetUserOnlineStatus Tests

    [Fact]
    public async Task GetUserOnlineStatus_ShouldCallPresenceService()
    {
        // Arrange
        var userId = "other-user-123";
        _presenceServiceMock
            .Setup(x => x.IsUserOnlineAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _chatHub.GetUserOnlineStatus(userId);

        // Assert
        _presenceServiceMock.Verify(
            x => x.IsUserOnlineAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserOnlineStatus_WhenUserOffline_ShouldReturnFalse()
    {
        // Arrange
        var userId = "other-user-123";
        _presenceServiceMock
            .Setup(x => x.IsUserOnlineAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _chatHub.GetUserOnlineStatus(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetOnlineUsers Tests

    [Fact]
    public async Task GetOnlineUsers_ShouldCallPresenceService()
    {
        // Arrange
        var onlineUsers = new List<string> { "user1", "user2", "user3" };
        _presenceServiceMock
            .Setup(x => x.GetOnlineUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(onlineUsers);

        // Act
        var result = await _chatHub.GetOnlineUsers();

        // Assert
        _presenceServiceMock.Verify(
            x => x.GetOnlineUsersAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        result.Should().BeEquivalentTo(onlineUsers);
    }

    [Fact]
    public async Task GetOnlineUsers_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _presenceServiceMock
            .Setup(x => x.GetOnlineUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _chatHub.GetOnlineUsers();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserLastSeen Tests

    [Fact]
    public async Task GetUserLastSeen_ShouldCallPresenceService()
    {
        // Arrange
        var userId = "other-user-123";
        var lastSeen = DateTime.UtcNow.AddMinutes(-5);
        _presenceServiceMock
            .Setup(x => x.GetLastSeenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastSeen);

        // Act
        var result = await _chatHub.GetUserLastSeen(userId);

        // Assert
        _presenceServiceMock.Verify(
            x => x.GetLastSeenAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        result.Should().Be(lastSeen);
    }

    [Fact]
    public async Task GetUserLastSeen_WhenNeverSeen_ShouldReturnNull()
    {
        // Arrange
        var userId = "new-user-123";
        _presenceServiceMock
            .Setup(x => x.GetLastSeenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        // Act
        var result = await _chatHub.GetUserLastSeen(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}

