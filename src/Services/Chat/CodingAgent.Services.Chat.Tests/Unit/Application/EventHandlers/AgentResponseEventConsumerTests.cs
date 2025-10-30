using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Application.EventHandlers;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.SharedKernel.Domain.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Application.EventHandlers;

[Trait("Category", "Unit")]
public class AgentResponseEventConsumerTests
{
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock;
    private readonly Mock<ILogger<AgentResponseEventConsumer>> _loggerMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly AgentResponseEventConsumer _consumer;

    public AgentResponseEventConsumerTests()
    {
        _conversationServiceMock = new Mock<IConversationService>();
        _hubContextMock = new Mock<IHubContext<ChatHub>>();
        _loggerMock = new Mock<ILogger<AgentResponseEventConsumer>>();
        _clientProxyMock = new Mock<IClientProxy>();
        _hubClientsMock = new Mock<IHubClients>();

        // Setup hub context clients
        _hubClientsMock
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_clientProxyMock.Object);

        _hubContextMock
            .Setup(h => h.Clients)
            .Returns(_hubClientsMock.Object);

        _consumer = new AgentResponseEventConsumer(
            _conversationServiceMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldPersistAgentMessage()
    {
        // Arrange
        var evt = new AgentResponseEvent
        {
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Content = "Agent response",
            GeneratedAt = DateTime.UtcNow,
            TokensUsed = 150,
            Model = "gpt-4o"
        };

        var message = new Message(
            evt.ConversationId,
            null, // Agent messages have no user
            evt.Content,
            MessageRole.Assistant);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(
                evt.ConversationId,
                null,
                evt.Content,
                MessageRole.Assistant,
                default))
            .ReturnsAsync(message);

        var contextMock = new Mock<ConsumeContext<AgentResponseEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _conversationServiceMock.Verify(
            s => s.AddMessageAsync(
                evt.ConversationId,
                null,
                evt.Content,
                MessageRole.Assistant,
                default),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldBroadcastViaSignalR()
    {
        // Arrange
        var evt = new AgentResponseEvent
        {
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Content = "Agent response",
            GeneratedAt = DateTime.UtcNow,
            TokensUsed = 200,
            Model = "gpt-4o-mini"
        };

        var message = new Message(
            evt.ConversationId,
            null,
            evt.Content,
            MessageRole.Assistant);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(
                It.IsAny<Guid>(),
                null,
                It.IsAny<string>(),
                MessageRole.Assistant,
                default))
            .ReturnsAsync(message);

        var contextMock = new Mock<ConsumeContext<AgentResponseEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        // Capture SendCoreAsync calls
        var sendAsyncCalls = new List<(string method, object[] args)>();
        _clientProxyMock
            .Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default))
            .Callback<string, object[], CancellationToken>((method, args, ct) =>
            {
                sendAsyncCalls.Add((method, args));
            })
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert: Verify ReceiveMessage was sent
        var receiveMessageCall = sendAsyncCalls.FirstOrDefault(c => c.method == "ReceiveMessage");
        receiveMessageCall.Should().NotBe(default);
        receiveMessageCall.args.Should().HaveCount(1);

        // Verify the message structure
        var messageArg = receiveMessageCall.args[0];
        messageArg.Should().NotBeNull();
        
        var contentProp = messageArg.GetType().GetProperty("Content");
        contentProp.Should().NotBeNull();
        contentProp!.GetValue(messageArg).Should().Be(evt.Content);

        var roleProp = messageArg.GetType().GetProperty("Role");
        roleProp.Should().NotBeNull();
        roleProp!.GetValue(messageArg).Should().Be("Assistant");

        var userIdProp = messageArg.GetType().GetProperty("UserId");
        userIdProp.Should().NotBeNull();
        userIdProp!.GetValue(messageArg).Should().BeNull();

        // Verify AgentTyping false was sent
        var agentTypingCall = sendAsyncCalls.FirstOrDefault(c => c.method == "AgentTyping");
        agentTypingCall.Should().NotBe(default);
        agentTypingCall.args.Should().HaveCount(1);
        agentTypingCall.args[0].Should().Be(false);

        // Verify SendCoreAsync was called twice (ReceiveMessage + AgentTyping)
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Consume_ShouldLogInformation()
    {
        // Arrange
        var evt = new AgentResponseEvent
        {
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Content = "Test content",
            GeneratedAt = DateTime.UtcNow
        };

        var message = new Message(evt.ConversationId, null, evt.Content, MessageRole.Assistant);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), null, It.IsAny<string>(), MessageRole.Assistant, default))
            .ReturnsAsync(message);

        var contextMock = new Mock<ConsumeContext<AgentResponseEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert: Verify logging occurred (at least 3 information logs)
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(3));
    }

    [Fact]
    public async Task Consume_WhenServiceThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var evt = new AgentResponseEvent
        {
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Content = "Test content",
            GeneratedAt = DateTime.UtcNow
        };

        var exception = new InvalidOperationException("Test exception");
        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), null, It.IsAny<string>(), MessageRole.Assistant, default))
            .ThrowsAsync(exception);

        var contextMock = new Mock<ConsumeContext<AgentResponseEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _consumer.Consume(contextMock.Object));

        // Verify error was logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldBroadcastToCorrectGroup()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var evt = new AgentResponseEvent
        {
            ConversationId = conversationId,
            MessageId = Guid.NewGuid(),
            Content = "Agent response",
            GeneratedAt = DateTime.UtcNow
        };

        var message = new Message(evt.ConversationId, null, evt.Content, MessageRole.Assistant);

        _conversationServiceMock
            .Setup(s => s.AddMessageAsync(It.IsAny<Guid>(), null, It.IsAny<string>(), MessageRole.Assistant, default))
            .ReturnsAsync(message);

        var contextMock = new Mock<ConsumeContext<AgentResponseEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert: Verify Group was called with correct conversation ID
        _hubClientsMock.Verify(
            c => c.Group(conversationId.ToString()),
            Times.Exactly(2)); // Once for ReceiveMessage, once for AgentTyping
    }
}
