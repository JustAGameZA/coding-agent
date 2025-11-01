using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.SharedKernel.Domain.Events;
using FluentAssertions;
using MassTransit;
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
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly ChatHub _chatHub;
    private readonly ClaimsPrincipal _testUser;
    private static readonly Guid TestUserGuid = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly string TestUserId = TestUserGuid.ToString();
    private const string TestConnectionId = "test-connection-id";

    public ChatHubTests()
    {
        _testLogger = new TestLogger<ChatHub>();
        _conversationServiceMock = new Mock<IConversationService>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
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
        _chatHub = new ChatHub(_conversationServiceMock.Object, _conversationRepositoryMock.Object, _publishEndpointMock.Object, _testLogger)
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
        var hub = new ChatHub(_conversationServiceMock.Object, _conversationRepositoryMock.Object, _publishEndpointMock.Object, _testLogger);

        // Assert
        hub.Should().NotBeNull();
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
        infoLogs.Any(r => r.Message.Contains(TestUserGuid.ToString())
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
        infoLogs.Any(r => r.Message.Contains(TestUserGuid.ToString())
                       && r.Message.Contains(TestConnectionId)
                       && r.Message.Contains(conversationId)
                       && r.Message.Contains("left", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    #endregion

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_ShouldPersistMessage()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "Hello, world!";
        var userId = Guid.Parse(TestUserId);

        var message = new Message(
            Guid.Parse(conversationId),
            userId,
            content,
            MessageRole.User);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(
                It.IsAny<Guid>(),
                userId,
                content,
                MessageRole.User,
                default))
            .ReturnsAsync(message);

        // Act
        await _chatHub.SendMessage(conversationId, content);

        // Assert
        _conversationServiceMock.Verify(
            s => s.AddMessageAsync(
                Guid.Parse(conversationId),
                userId,
                content,
                MessageRole.User,
                default),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_ShouldLogAction()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "Hello, world!";
        var userId = TestUserGuid;

        var message = new Message(Guid.Parse(conversationId), userId, content, MessageRole.User);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), userId, It.IsAny<string>(), MessageRole.User, default))
            .ReturnsAsync(message);

        // Act
        await _chatHub.SendMessage(conversationId, content);

        // Assert
        var infoLogs = _testLogger.Records.Where(r => r.LogLevel == LogLevel.Information).ToList();
        infoLogs.Any(r => r.Message.Contains(TestUserGuid.ToString())
                       && r.Message.Contains(conversationId)
                       && r.Message.Contains("sent message", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_ShouldBroadcastToGroup()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "Hello, world!";
        var userId = Guid.Parse(TestUserId);

        var message = new Message(Guid.Parse(conversationId), userId, content, MessageRole.User);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), userId, It.IsAny<string>(), MessageRole.User, default))
            .ReturnsAsync(message);

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
            Times.AtLeastOnce);

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
        var conversationId = Guid.NewGuid().ToString();
        var content = "Hello, world!";
        var userId = Guid.Parse(TestUserId);
        object? capturedData = null;

        var message = new Message(Guid.Parse(conversationId), userId, content, MessageRole.User);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), userId, It.IsAny<string>(), MessageRole.User, default))
            .ReturnsAsync(message);

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

        var userIdProp = capturedData!.GetType().GetProperty("UserId")?.GetValue(capturedData);
        userIdProp.Should().Be(userId);

        var convIdProp = capturedData.GetType().GetProperty("ConversationId")?.GetValue(capturedData);
        convIdProp.Should().Be(Guid.Parse(conversationId));

        var messageContent = capturedData.GetType().GetProperty("Content")?.GetValue(capturedData);
        messageContent.Should().Be(content);

        var sentAt = capturedData.GetType().GetProperty("SentAt")?.GetValue(capturedData);
        sentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SendMessage_ShouldEmitAgentTypingTrue()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "Test message";
        var userId = Guid.Parse(TestUserId);

        var message = new Message(Guid.Parse(conversationId), userId, content, MessageRole.User);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), userId, content, MessageRole.User, default))
            .ReturnsAsync(message);

        var sendAsyncCalls = new List<(string method, object?[] args)>();
        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, ct) =>
            {
                sendAsyncCalls.Add((method, args!));
            })
            .Returns(Task.CompletedTask);

        // Act
        await _chatHub.SendMessage(conversationId, content);

        // Assert: Verify AgentTyping was sent with true
        var agentTypingCall = sendAsyncCalls.FirstOrDefault(c => c.method == "AgentTyping");
        agentTypingCall.Should().NotBe(default);
        agentTypingCall.args.Should().HaveCount(1);
        agentTypingCall.args[0].Should().Be(true);

        // Verify MessageSentEvent was published
        _publishEndpointMock.Verify(
            p => p.Publish(
                It.Is<MessageSentEvent>(e =>
                    e.ConversationId == Guid.Parse(conversationId) &&
                    e.UserId == userId &&
                    e.Content == content),
                default),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_ShouldPublishMessageSentEvent()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "Test message";
        var userId = Guid.Parse(TestUserId);

        var message = new Message(Guid.Parse(conversationId), userId, content, MessageRole.User);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), userId, content, MessageRole.User, default))
            .ReturnsAsync(message);

        // Act
        await _chatHub.SendMessage(conversationId, content);

        // Assert
        _publishEndpointMock.Verify(
            p => p.Publish(
                It.Is<MessageSentEvent>(e =>
                    e.ConversationId == Guid.Parse(conversationId) &&
                    e.MessageId == message.Id &&
                    e.UserId == userId &&
                    e.Content == content &&
                    e.Role == "User"),
                default),
            Times.Once);
    }

    #endregion
}

