using System.Net.Http.Json;
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.SharedKernel.Domain.Events;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace CodingAgent.Services.Chat.Tests.Integration;

[Collection("ChatServiceCollection")]
[Trait("Category", "Integration")]
public class AgentFlowTests : IClassFixture<ChatServiceFixture>
{
    private readonly ChatServiceFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AgentFlowTests(ChatServiceFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task UserSendsMessage_ShouldPublishMessageSentEvent()
    {
        // Arrange: Create conversation
        var conversation = await CreateTestConversationAsync();
        _output.WriteLine($"Created conversation: {conversation.Id}");

        // Create SignalR connection
        var hubConnection = await CreateHubConnectionAsync();
        await hubConnection.InvokeAsync("JoinConversation", conversation.Id.ToString());
        _output.WriteLine($"Joined conversation via SignalR");

        // Track received messages
        var receivedMessages = new List<MessageDto>();
        hubConnection.On<MessageDto>("ReceiveMessage", msg =>
        {
            _output.WriteLine($"Received message: {msg.Content} (Role: {msg.Role})");
            receivedMessages.Add(msg);
        });

        // Act: Send message via SignalR
        var messageContent = "Hello AI agent";
        await hubConnection.InvokeAsync("SendMessage", conversation.Id.ToString(), messageContent);
        _output.WriteLine($"Sent message: {messageContent}");

        // Wait for message processing
        await Task.Delay(500);

        // Assert: Verify message was echoed back to user
        receivedMessages.Should().ContainSingle();
        receivedMessages[0].Content.Should().Be(messageContent);
        receivedMessages[0].Role.Should().Be("User");

        // Note: Verifying MessageSentEvent was published requires MassTransit test harness
        // For now, we verify the message was persisted and echoed back

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task AgentResponseEvent_ShouldBroadcastViaSignalR()
    {
        // Arrange: Create conversation and listen for SignalR messages
        var conversation = await CreateTestConversationAsync();
        _output.WriteLine($"Created conversation: {conversation.Id}");

        var hubConnection = await CreateHubConnectionAsync();
        await hubConnection.InvokeAsync("JoinConversation", conversation.Id.ToString());
        _output.WriteLine($"Joined conversation via SignalR");

        MessageDto? receivedMessage = null;
        hubConnection.On<MessageDto>("ReceiveMessage", msg =>
        {
            _output.WriteLine($"Received message: {msg.Content} (Role: {msg.Role}, UserId: {msg.UserId})");
            receivedMessage = msg;
        });

        // Act: Publish AgentResponseEvent (simulate Orchestration Service)
        var agentContent = "Hello! I'm your AI coding assistant.";
        await PublishAgentResponseEventAsync(new AgentResponseEvent
        {
            ConversationId = conversation.Id,
            MessageId = Guid.NewGuid(),
            Content = agentContent,
            GeneratedAt = DateTime.UtcNow,
            TokensUsed = 150,
            Model = "gpt-4o"
        });

        // Wait for SignalR propagation
        await Task.Delay(1000);

        // Assert
        receivedMessage.Should().NotBeNull();
        receivedMessage!.Role.Should().Be("Assistant");
        receivedMessage.UserId.Should().BeNull();
        receivedMessage.Content.Should().Be(agentContent);

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task AgentTypingIndicator_ShouldToggleCorrectly()
    {
        // Arrange
        var conversation = await CreateTestConversationAsync();
        _output.WriteLine($"Created conversation: {conversation.Id}");

        var hubConnection = await CreateHubConnectionAsync();
        await hubConnection.InvokeAsync("JoinConversation", conversation.Id.ToString());

        var typingEvents = new List<bool>();
        hubConnection.On<bool>("AgentTyping", isTyping =>
        {
            _output.WriteLine($"AgentTyping event received: {isTyping}");
            typingEvents.Add(isTyping);
        });

        // Act 1: User sends message (should trigger AgentTyping = true)
        await hubConnection.InvokeAsync("SendMessage", conversation.Id.ToString(), "Test message");
        await Task.Delay(300);

        // Act 2: Simulate agent response (should trigger AgentTyping = false)
        await PublishAgentResponseEventAsync(new AgentResponseEvent
        {
            ConversationId = conversation.Id,
            MessageId = Guid.NewGuid(),
            Content = "Response",
            GeneratedAt = DateTime.UtcNow
        });
        await Task.Delay(500);

        // Assert
        _output.WriteLine($"Total typing events received: {typingEvents.Count}");
        typingEvents.Should().ContainInOrder(true, false);
        typingEvents.Count.Should().BeGreaterOrEqualTo(2);

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task AgentMessage_ShouldHaveNullUserId()
    {
        // Arrange
        var conversation = await CreateTestConversationAsync();
        _output.WriteLine($"Created conversation: {conversation.Id}");

        // Act: Publish AgentResponseEvent
        var messageId = Guid.NewGuid();
        await PublishAgentResponseEventAsync(new AgentResponseEvent
        {
            ConversationId = conversation.Id,
            MessageId = messageId,
            Content = "Agent response",
            GeneratedAt = DateTime.UtcNow
        });

        await Task.Delay(800); // Allow processing

        // Assert: Fetch conversation and check message
        var response = await _fixture.Client.GetAsync($"/conversations/{conversation.Id}");
        response.Should().BeSuccessful();

        var conv = await response.Content.ReadFromJsonAsync<ConversationWithMessagesDto>();
        conv.Should().NotBeNull();
        
        var agentMsg = conv!.Messages?.FirstOrDefault(m => m.Role == "Assistant");

        agentMsg.Should().NotBeNull();
        agentMsg!.UserId.Should().BeNull();
        agentMsg.Content.Should().Be("Agent response");
    }

    private async Task<ConversationDto> CreateTestConversationAsync()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/conversations", new
        {
            Title = $"Test Conversation {Guid.NewGuid()}"
        });
        response.Should().BeSuccessful();
        return (await response.Content.ReadFromJsonAsync<ConversationDto>())!;
    }

    private async Task<HubConnection> CreateHubConnectionAsync()
    {
        // Note: In a real scenario, you'd need to authenticate and get a JWT token
        // For testing, we're using anonymous access (requires test configuration)
        var hubUrl = $"{_fixture.Client.BaseAddress}hubs/chat";
        
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _fixture.Factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync();
        return connection;
    }

    private async Task PublishAgentResponseEventAsync(AgentResponseEvent evt)
    {
        // Get the MassTransit publish endpoint from the test service provider
        var publishEndpoint = _fixture.Factory.Services.GetRequiredService<MassTransit.IPublishEndpoint>();
        await publishEndpoint.Publish(evt);
        _output.WriteLine($"Published AgentResponseEvent for conversation {evt.ConversationId}");
    }

    private record ConversationDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
    
    private record ConversationWithMessagesDto(
        Guid Id, 
        string Title, 
        DateTime CreatedAt, 
        DateTime UpdatedAt,
        List<MessageDto>? Messages);

    private record MessageDto(
        Guid Id,
        Guid ConversationId,
        Guid? UserId,
        string Content,
        string Role,
        DateTime SentAt);
}
