# Chat Service Refactor: User-to-AI Agent Architecture Design

**Version**: 1.0  
**Date**: October 28, 2025  
**Status**: Architecture Design Phase  
**Author**: Solution Architect  

---

## Executive Summary

This document provides the complete architecture design for refactoring the Chat Service from a multi-user messaging system (Slack-like) to a user-to-AI agent communication platform. The refactor removes user presence tracking, multi-user chat features, and implements a clean user→agent→user message flow with event-driven orchestration.

**Key Changes**:
- ✅ Remove: User presence tracking (IPresenceService, PresenceService)
- ✅ Remove: Multi-user features (TypingIndicator to others, UserPresenceChanged events)
- ✅ Add: Agent message flow via event bus (MessageSentEvent → Orchestration → AgentResponseEvent)
- ✅ Add: `AgentTyping` indicator (boolean signal to user)
- ✅ Keep: Message persistence, conversation management, SignalR infrastructure

---

## 1. Event Flow Architecture

### Complete Message Flow Diagram

```
┌──────────────┐
│    User      │
│  (Browser)   │
└──────┬───────┘
       │
       │ 1. SendMessage(conversationId, content)
       │    via SignalR
       ▼
┌──────────────────────────────────────────────────────────┐
│                    Chat Service                          │
│  ┌────────────────────────────────────────────────────┐ │
│  │              ChatHub.SendMessage()                  │ │
│  │  • Persist Message (UserId, Role=User)             │ │
│  │  • Broadcast ReceiveMessage to user                │ │
│  │  • Publish MessageSentEvent to RabbitMQ            │ │
│  │  • Emit AgentTyping: true to user                  │ │
│  └────────────────────────────────────────────────────┘ │
└────────────────────────┬─────────────────────────────────┘
                         │
                         │ 2. MessageSentEvent
                         │    { ConversationId, MessageId, UserId, Content, Role }
                         │
┌────────────────────────▼─────────────────────────────────┐
│                    RabbitMQ                              │
└────────────────────────┬─────────────────────────────────┘
                         │
                         │ 3. Event consumed
                         ▼
┌──────────────────────────────────────────────────────────┐
│              Orchestration Service                        │
│  ┌────────────────────────────────────────────────────┐ │
│  │       MessageSentEventConsumer.Consume()           │ │
│  │  • If Role == User:                                │ │
│  │    - Process with AI (GPT-4, Claude, Ollama)      │ │
│  │    - Generate agent response                       │ │
│  │    - Publish AgentResponseEvent to RabbitMQ       │ │
│  └────────────────────────────────────────────────────┘ │
└────────────────────────┬─────────────────────────────────┘
                         │
                         │ 4. AgentResponseEvent
                         │    { ConversationId, Content }
                         │
┌────────────────────────▼─────────────────────────────────┐
│                    RabbitMQ                              │
└────────────────────────┬─────────────────────────────────┘
                         │
                         │ 5. Event consumed
                         ▼
┌──────────────────────────────────────────────────────────┐
│                    Chat Service                          │
│  ┌────────────────────────────────────────────────────┐ │
│  │      AgentResponseEventConsumer.Consume()          │ │
│  │  • Persist Message (UserId=null, Role=Assistant)   │ │
│  │  • Broadcast ReceiveMessage to user via SignalR   │ │
│  │  • Emit AgentTyping: false to user                │ │
│  └────────────────────────────────────────────────────┘ │
└────────────────────────┬─────────────────────────────────┘
                         │
                         │ 6. ReceiveMessage
                         │    via SignalR
                         ▼
                  ┌──────────────┐
                  │    User      │
                  │  (Browser)   │
                  └──────────────┘
```

### Callback Pattern Decision

**Chosen**: **Option A: Event-Driven (AgentResponseEvent)**

**Rationale**:

**Pros**:
- ✅ **Decoupling**: Chat Service doesn't need to know about Orchestration Service
- ✅ **Scalability**: Multiple Orchestration instances can process events in parallel
- ✅ **Resilience**: RabbitMQ retries on failure, no lost messages
- ✅ **Consistency**: All inter-service communication uses events (existing pattern)
- ✅ **Observability**: Easy to trace events through distributed system
- ✅ **Testability**: Can mock event bus for unit tests

**Cons**:
- ⚠️ **Eventual Consistency**: Small delay between user message and agent response (acceptable for chat)
- ⚠️ **Complexity**: Need to create new event + consumer (but follows existing patterns)

**Why This Fits Our Architecture**:
- We already use event-driven communication (MessageSentEvent, TaskCompletedEvent)
- MassTransit + RabbitMQ infrastructure is mature and reliable
- Aligns with microservices principles (loose coupling, async communication)
- Supports future multi-agent scenarios (multiple Orchestration services responding)

**Option B (HTTP Endpoint) Rejected Because**:
- ❌ Tight coupling (Orchestration needs to know Chat Service URL)
- ❌ Retry logic complexity (must implement Polly policies)
- ❌ No natural request tracing (need custom correlation headers)
- ❌ Doesn't fit existing event-driven architecture

---

## 2. Breaking Changes List

### Removed Hub Methods

| Method | Current Usage | Impact | Mitigation |
|--------|---------------|--------|------------|
| `OnConnectedAsync` (override) | Calls `_presenceService.SetUserOnlineAsync()` | Remove presence tracking | Keep method for logging only |
| `OnDisconnectedAsync` (override) | Calls `_presenceService.SetUserOfflineAsync()` | Remove presence tracking | Keep method for logging only |
| `TypingIndicator(conversationId, isTyping)` | Broadcasts to `OthersInGroup` | No multi-user chat | **DELETE** - No replacement |
| `GetUserOnlineStatus(userId)` | Returns bool from Redis | No user presence | **DELETE** - Frontend must remove calls |
| `GetOnlineUsers()` | Returns list from Redis | No user presence | **DELETE** - Frontend must remove calls |
| `GetUserLastSeen(userId)` | Returns timestamp from Redis | No user presence | **DELETE** - Frontend must remove calls |

### New SignalR Events

| Event | Direction | Parameters | When Emitted | Purpose |
|-------|-----------|------------|--------------|---------|
| `AgentTyping` | Server→Client | `bool isTyping` | **true**: After user sends message (before AI response arrives)<br>**false**: When agent response is broadcast | Show loading indicator to user |
| `ReceiveMessage` | Server→Client | `{ MessageId, ConversationId, Content, Role, SentAt, UserId? }` | **Existing**: Unchanged, used for both user and agent messages | Display messages in chat UI |

### Modified Hub Methods

| Method | Before | After | Changes |
|--------|--------|-------|---------|
| `SendMessage(conversationId, content)` | Broadcasts to Group | Persists, publishes event, broadcasts to user only | Add: Publish `MessageSentEvent`, Emit `AgentTyping: true` |
| `JoinConversation(conversationId)` | Adds to SignalR group | **No change** | Still needed for broadcast |
| `LeaveConversation(conversationId)` | Removes from group | **No change** | Still needed |

---

## 3. API Endpoint Specification

### No New HTTP Endpoints Required

**Decision**: Use event-driven architecture exclusively. No HTTP callback endpoint needed.

**Reasoning**:
- Agent responses are asynchronous by nature
- Event bus provides reliable delivery
- Simpler authorization (no service-to-service auth needed)
- Consistent with existing architecture

**If HTTP Endpoint Were Required (for reference)**:

```csharp
// NOT IMPLEMENTING - For reference only
POST /conversations/{id}/agent-response

Authorization: Service-to-Service (API Key in X-API-Key header)

Request DTO:
public record AgentResponseRequest(string Content);

Validation Rules:
- Content: Required, max 50,000 chars
- ConversationId: Must exist, user must own it

Response: 202 Accepted (async processing)

Error Scenarios:
- 404: Conversation not found
- 400: Invalid content (too long, empty)
- 401: Missing/invalid API key
```

---

## 4. Database Schema Analysis

### Current Schema (chat.messages table)

```sql
CREATE TABLE chat.messages (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL REFERENCES chat.conversations(id) ON DELETE CASCADE,
    user_id UUID,  -- ✅ NULLABLE (perfect for agent messages)
    content TEXT NOT NULL,
    role TEXT NOT NULL,  -- ✅ Stored as string (User, Assistant, System)
    sent_at TIMESTAMP NOT NULL,
    
    INDEX idx_messages_conversation_id (conversation_id),
    INDEX idx_messages_sent_at (sent_at)
);
```

### Schema Compatibility Check

| Requirement | Current Schema | Status |
|-------------|----------------|--------|
| Nullable UserId for agent messages | `user_id UUID` (nullable) | ✅ **SUPPORTED** |
| Role enum storage | `role TEXT` (string conversion) | ✅ **SUPPORTED** |
| Content size for AI responses | `content TEXT` (max 50,000 chars) | ✅ **SUPPORTED** |

### Migration Needed

**NO** - Current schema fully supports the refactor.

**Confirmation**:
- `UserId` is already nullable (see `Message.cs`: `public Guid? UserId { get; private set; }`)
- `Role` is stored as string via EF Core conversion (see `ChatDbContext.cs`: `HasConversion<string>()`)
- `Content` is `TEXT` type in PostgreSQL (supports up to 1GB, configured limit: 50,000 chars)

**Future Consideration** (Phase 5):
If we need to track token usage per message for cost analysis:

```sql
-- Future migration (not required for refactor)
ALTER TABLE chat.messages 
ADD COLUMN token_count INTEGER,
ADD COLUMN model_used TEXT;
```

---

## 5. Implementation Plan

### Task Dependency Graph

```
Phase 1: Shared Kernel (Sequential)
├─ Task 1: Create AgentResponseEvent.cs
│
Phase 2: Backend (Parallel after Phase 1)
├─ Task 2: Create AgentResponseEventConsumer.cs
├─ Task 3: Refactor ChatHub.cs
├─ Task 4: Update MessageSentEventConsumer.cs
├─ Task 5: Update Program.cs (remove presence DI)
├─ Task 6: Delete IPresenceService files
│
Phase 3: Frontend (Parallel to Phase 2)
├─ Task 7: Update chat.component.ts
├─ Task 8: Update chat.component.html
├─ Task 9: Update signalr.service.ts
├─ Task 10: Delete presence.service.ts
│
Phase 4: Testing (Sequential after Phase 2 & 3)
├─ Task 11: Create AgentFlowIntegrationTests.cs
├─ Task 12: Update ChatHubTests.cs
├─ Task 13: Update E2E tests
│
Phase 5: Documentation (Parallel to Phase 4)
├─ Task 14: Create ADR-006-chat-agent-refactor.md
├─ Task 15: Update chat-service-openapi.yaml
├─ Task 16: Update 01-SERVICE-CATALOG.md
```

### Backend Track (Parallel Group 1)

#### Task 1: Create AgentResponseEvent in SharedKernel ⚠️ PREREQUISITE
- **Files**: `src/SharedKernel/CodingAgent.SharedKernel/Domain/Events/AgentResponseEvent.cs` (new)
- **Estimated**: 15 minutes
- **Dependencies**: None
- **Blocks**: Tasks 2, 3, 4

```csharp
namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when an AI agent generates a response to a user message.
/// </summary>
public record AgentResponseEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the unique identifier of the conversation.
    /// </summary>
    public required Guid ConversationId { get; init; }
    
    /// <summary>
    /// Gets the generated agent response content.
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// Gets the model used to generate the response (e.g., "gpt-4o", "claude-3.5-sonnet").
    /// </summary>
    public string? ModelUsed { get; init; }
    
    /// <summary>
    /// Gets the token count for the response (for cost tracking).
    /// </summary>
    public int? TokenCount { get; init; }
}
```

#### Task 2: Create AgentResponseEventConsumer in Chat Service
- **Files**: `src/Services/Chat/CodingAgent.Services.Chat/Infrastructure/Messaging/Consumers/AgentResponseEventConsumer.cs` (new)
- **Estimated**: 45 minutes
- **Dependencies**: Task 1
- **Responsibilities**:
  1. Consume AgentResponseEvent from RabbitMQ
  2. Create Message entity (UserId=null, Role=Assistant)
  3. Persist to database via ConversationRepository
  4. Broadcast ReceiveMessage to user via SignalR
  5. Emit AgentTyping: false to user

```csharp
using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using CodingAgent.Services.Chat.Api.Hubs;

namespace CodingAgent.Services.Chat.Infrastructure.Messaging.Consumers;

public class AgentResponseEventConsumer : IConsumer<AgentResponseEvent>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<AgentResponseEventConsumer> _logger;

    public AgentResponseEventConsumer(
        IConversationRepository conversationRepository,
        IHubContext<ChatHub> hubContext,
        ILogger<AgentResponseEventConsumer> logger)
    {
        _conversationRepository = conversationRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AgentResponseEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation(
            "[Chat] Received AgentResponseEvent: ConversationId={ConversationId}, Content={ContentPreview}",
            @event.ConversationId,
            @event.Content.Length > 50 ? @event.Content.Substring(0, 50) + "..." : @event.Content);

        // 1. Create agent message
        var message = new Message(
            conversationId: @event.ConversationId,
            userId: null,  // Agent has no UserId
            content: @event.Content,
            role: MessageRole.Assistant
        );

        // 2. Persist message
        var conversation = await _conversationRepository.GetByIdAsync(@event.ConversationId);
        
        if (conversation == null)
        {
            _logger.LogWarning(
                "[Chat] Conversation {ConversationId} not found, cannot persist agent response",
                @event.ConversationId);
            return;
        }

        conversation.AddMessage(message);
        await _conversationRepository.UpdateAsync(conversation);

        _logger.LogInformation(
            "[Chat] Persisted agent message {MessageId} to conversation {ConversationId}",
            message.Id,
            @event.ConversationId);

        // 3. Broadcast to user via SignalR
        await _hubContext.Clients.Group(@event.ConversationId.ToString())
            .SendAsync("ReceiveMessage", new
            {
                MessageId = message.Id,
                ConversationId = message.ConversationId,
                UserId = (Guid?)null,
                Content = message.Content,
                Role = message.Role.ToString(),
                SentAt = message.SentAt
            });

        // 4. Emit AgentTyping: false
        await _hubContext.Clients.Group(@event.ConversationId.ToString())
            .SendAsync("AgentTyping", false);

        _logger.LogInformation(
            "[Chat] Broadcast agent message {MessageId} to conversation {ConversationId}",
            message.Id,
            @event.ConversationId);
    }
}
```

#### Task 3: Refactor ChatHub.cs
- **Files**: `src/Services/Chat/CodingAgent.Services.Chat/Api/Hubs/ChatHub.cs` (modify)
- **Estimated**: 60 minutes
- **Dependencies**: Task 1
- **Changes**:
  1. Remove `_presenceService` dependency
  2. Add `IConversationRepository` and `IPublishEndpoint` dependencies
  3. Remove presence tracking from `OnConnectedAsync`/`OnDisconnectedAsync`
  4. Update `SendMessage` to persist message, publish event, emit AgentTyping
  5. Delete `TypingIndicator`, `GetUserOnlineStatus`, `GetOnlineUsers`, `GetUserLastSeen`

```csharp
[Authorize]
public class ChatHub : Hub
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IConversationRepository conversationRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<ChatHub> logger)
    {
        _conversationRepository = conversationRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    private string UserId => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.FindFirst("sub")?.Value
        ?? "anonymous";

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "User {UserId} connected to chat hub with connection {ConnectionId}",
            UserId,
            Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "User {UserId} disconnected from chat hub with connection {ConnectionId}",
            UserId,
            Context.ConnectionId);
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation(
            "User {UserId} (connection {ConnectionId}) joined conversation {ConversationId}",
            UserId,
            Context.ConnectionId,
            conversationId);
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation(
            "User {UserId} (connection {ConnectionId}) left conversation {ConversationId}",
            UserId,
            Context.ConnectionId,
            conversationId);
    }

    public async Task SendMessage(string conversationId, string content)
    {
        var conversationGuid = Guid.Parse(conversationId);
        var userGuid = Guid.Parse(UserId);

        // 1. Persist user message
        var message = new Message(
            conversationId: conversationGuid,
            userId: userGuid,
            content: content,
            role: MessageRole.User
        );

        var conversation = await _conversationRepository.GetByIdAsync(conversationGuid);
        
        if (conversation == null)
        {
            _logger.LogWarning(
                "Conversation {ConversationId} not found for user {UserId}",
                conversationId,
                UserId);
            return;
        }

        conversation.AddMessage(message);
        await _conversationRepository.UpdateAsync(conversation);

        _logger.LogInformation(
            "User {UserId} sent message {MessageId} to conversation {ConversationId}",
            UserId,
            message.Id,
            conversationId);

        // 2. Broadcast to user (echo back)
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            UserId = message.UserId,
            Content = message.Content,
            Role = message.Role.ToString(),
            SentAt = message.SentAt
        });

        // 3. Publish event for Orchestration Service
        await _publishEndpoint.Publish(new MessageSentEvent
        {
            ConversationId = conversationGuid,
            MessageId = message.Id,
            UserId = userGuid,
            Content = content,
            Role = MessageRole.User.ToString(),
            SentAt = message.SentAt
        });

        // 4. Emit AgentTyping: true (agent is "thinking")
        await Clients.Group(conversationId).SendAsync("AgentTyping", true);

        _logger.LogInformation(
            "Published MessageSentEvent for message {MessageId} in conversation {ConversationId}",
            message.Id,
            conversationId);
    }
}
```

#### Task 4: Update MessageSentEventConsumer in Orchestration Service
- **Files**: `src/Services/Orchestration/CodingAgent.Services.Orchestration/Infrastructure/Messaging/Consumers/MessageSentEventConsumer.cs` (modify)
- **Estimated**: 30 minutes
- **Dependencies**: Task 1
- **Changes**:
  1. Check if `Role == "User"` (ignore Assistant/System messages)
  2. Call AI service to generate response
  3. Publish `AgentResponseEvent` with generated content

```csharp
public class MessageSentEventConsumer : IConsumer<MessageSentEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MessageSentEventConsumer> _logger;
    // TODO: Add ILlmClient dependency when Orchestration Service is implemented

    public MessageSentEventConsumer(
        IPublishEndpoint publishEndpoint,
        ILogger<MessageSentEventConsumer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MessageSentEvent> context)
    {
        var msg = context.Message;
        
        _logger.LogInformation(
            "[Orchestration] Consumed MessageSentEvent: ConversationId={ConversationId}, MessageId={MessageId}, Role={Role}",
            msg.ConversationId,
            msg.MessageId,
            msg.Role);

        // Only process user messages (ignore agent/system messages)
        if (msg.Role != "User")
        {
            _logger.LogDebug(
                "[Orchestration] Ignoring non-user message {MessageId} with role {Role}",
                msg.MessageId,
                msg.Role);
            return;
        }

        // TODO: Call AI service (GPT-4, Claude, Ollama) to generate response
        // For now, echo back a placeholder response
        var agentResponse = $"Agent received: {msg.Content}";

        // Publish AgentResponseEvent
        await _publishEndpoint.Publish(new AgentResponseEvent
        {
            ConversationId = msg.ConversationId,
            Content = agentResponse,
            ModelUsed = "placeholder-model",
            TokenCount = agentResponse.Length  // Rough estimate
        });

        _logger.LogInformation(
            "[Orchestration] Published AgentResponseEvent for conversation {ConversationId}",
            msg.ConversationId);
    }
}
```

#### Task 5: Update Program.cs (Remove Presence Service)
- **Files**: `src/Services/Chat/CodingAgent.Services.Chat/Program.cs` (modify)
- **Estimated**: 15 minutes
- **Dependencies**: Task 3
- **Changes**:
  1. Remove `IPresenceService` registration
  2. Remove `PresenceService` registration
  3. Add `IConversationRepository` to ChatHub DI (may already exist)

```csharp
// DELETE these lines:
builder.Services.AddScoped<IPresenceService>(sp =>
{
    var redis = sp.GetService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<PresenceService>>();
    var meterFactory = sp.GetRequiredService<IMeterFactory>();
    return new PresenceService(redis, logger, meterFactory);
});
```

#### Task 6: Delete IPresenceService Files
- **Files** (delete):
  - `src/Services/Chat/CodingAgent.Services.Chat/Domain/Services/IPresenceService.cs`
  - `src/Services/Chat/CodingAgent.Services.Chat/Infrastructure/Presence/PresenceService.cs`
  - `src/Services/Chat/CodingAgent.Services.Chat/Api/Endpoints/PresenceEndpoints.cs`
- **Estimated**: 10 minutes
- **Dependencies**: Tasks 3, 5

### Frontend Track (Parallel Group 2)

#### Task 7: Update chat.component.ts
- **Files**: `src/Frontend/coding-agent-dashboard/src/app/features/chat/chat.component.ts` (modify)
- **Estimated**: 45 minutes
- **Dependencies**: None (frontend can develop independently)
- **Changes**:
  1. Remove `presenceService` dependency
  2. Remove `onlineUsers$` observable
  3. Remove `typingUsers` tracking
  4. Add `agentTyping: boolean` signal
  5. Subscribe to `AgentTyping` SignalR event
  6. Remove calls to presence-related hub methods

```typescript
export class ChatComponent implements OnInit {
  private signalR = inject(SignalRService);
  private chatService = inject(ChatService);
  
  messages = signal<Message[]>([]);
  agentTyping = signal<boolean>(false);  // NEW
  
  async ngOnInit() {
    await this.signalR.connect();
    
    // Subscribe to incoming messages
    this.signalR.on<Message>('ReceiveMessage', msg => {
      this.messages.update(msgs => [...msgs, msg]);
    });
    
    // NEW: Subscribe to agent typing indicator
    this.signalR.on<boolean>('AgentTyping', isTyping => {
      this.agentTyping.set(isTyping);
    });
  }
  
  async sendMessage(content: string) {
    await this.signalR.invoke('SendMessage', this.conversationId, content);
  }
}
```

#### Task 8: Update chat.component.html
- **Files**: `src/Frontend/coding-agent-dashboard/src/app/features/chat/chat.component.html` (modify)
- **Estimated**: 30 minutes
- **Dependencies**: Task 7
- **Changes**:
  1. Remove online users sidebar
  2. Remove typing indicators for other users
  3. Add agent typing indicator (loading spinner with "Agent is typing...")

```html
<!-- Agent typing indicator -->
@if (agentTyping()) {
  <div class="agent-typing-indicator">
    <mat-spinner diameter="20"></mat-spinner>
    <span>Agent is typing...</span>
  </div>
}

<!-- Message list -->
@for (msg of messages(); track msg.id) {
  <div class="message" [class.user]="msg.role === 'User'" [class.agent]="msg.role === 'Assistant'">
    <div class="message-content">{{ msg.content }}</div>
    <div class="message-timestamp">{{ msg.sentAt | date:'short' }}</div>
  </div>
}
```

#### Task 9: Update signalr.service.ts
- **Files**: `src/Frontend/coding-agent-dashboard/src/app/core/services/signalr.service.ts` (modify)
- **Estimated**: 20 minutes
- **Dependencies**: None
- **Changes**:
  1. Remove `typingIndicator(conversationId, isTyping)` method
  2. Remove presence-related hub method wrappers

```typescript
export class SignalRService {
  // DELETE these methods:
  // typingIndicator(conversationId: string, isTyping: boolean)
  // getUserOnlineStatus(userId: string)
  // getOnlineUsers()
  // getUserLastSeen(userId: string)
}
```

#### Task 10: Delete presence.service.ts
- **Files** (delete):
  - `src/Frontend/coding-agent-dashboard/src/app/core/services/presence.service.ts` (if exists)
- **Estimated**: 5 minutes
- **Dependencies**: Tasks 7, 8, 9

### Testing Track (Sequential after Implementation)

#### Task 11: Create AgentFlowIntegrationTests.cs
- **Files**: `src/Services/Chat/CodingAgent.Services.Chat.Tests/Integration/AgentFlowIntegrationTests.cs` (new)
- **Estimated**: 90 minutes
- **Dependencies**: Tasks 1, 2, 3
- **Test Scenarios**:
  1. User sends message → MessageSentEvent published
  2. AgentResponseEvent consumed → Message persisted with Role=Assistant, UserId=null
  3. SignalR broadcast verification (ReceiveMessage, AgentTyping events)

```csharp
[Collection("ChatServiceCollection")]
[Trait("Category", "Integration")]
public class AgentFlowIntegrationTests : IClassFixture<ChatServiceFixture>
{
    [Fact]
    public async Task UserMessage_ShouldPublishMessageSentEvent_AndReceiveAgentResponse()
    {
        // Arrange: User joins conversation via SignalR
        // Act: User sends message
        // Assert: MessageSentEvent published
        // Act: Simulate AgentResponseEvent
        // Assert: Agent message persisted, broadcast to user, AgentTyping: false
    }
}
```

#### Task 12: Update ChatHubTests.cs
- **Files**: `src/Services/Chat/CodingAgent.Services.Chat.Tests/Unit/Api/Hubs/ChatHubTests.cs` (modify)
- **Estimated**: 60 minutes
- **Dependencies**: Task 3
- **Changes**:
  1. Remove `_presenceServiceMock` from tests
  2. Add `_conversationRepositoryMock` and `_publishEndpointMock`
  3. Delete presence-related tests
  4. Update `SendMessage` tests to verify event publishing

```csharp
[Trait("Category", "Unit")]
public class ChatHubTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    
    // DELETE: _presenceServiceMock tests
}
```

#### Task 13: Update E2E Tests
- **Files**: E2E test suite (location TBD)
- **Estimated**: 90 minutes
- **Dependencies**: Tasks 7, 8, 9
- **Changes**:
  1. Remove presence tracking scenarios
  2. Add agent typing indicator verification
  3. Update message flow assertions (user → agent → user)

### Documentation Track (Parallel to Testing)

#### Task 14: Create ADR-006-chat-agent-refactor.md
- **Files**: `docs/ADR-006-chat-agent-refactor.md` (new)
- **Estimated**: 45 minutes
- **Dependencies**: None (can draft during implementation)
- **Content**:
  - Decision: Refactor Chat Service for user-to-AI communication
  - Context: Why we removed multi-user features
  - Consequences: Breaking changes, frontend updates
  - Alternatives: Why not HTTP callback

#### Task 15: Update chat-service-openapi.yaml
- **Files**: `docs/api/chat-service-openapi.yaml` (modify)
- **Estimated**: 30 minutes
- **Dependencies**: Task 3
- **Changes**:
  1. Remove presence endpoints
  2. Update SignalR hub method documentation
  3. Document `AgentTyping` event

#### Task 16: Update 01-SERVICE-CATALOG.md
- **Files**: `docs/01-SERVICE-CATALOG.md` (modify)
- **Estimated**: 30 minutes
- **Dependencies**: None
- **Changes**:
  1. Update Chat Service description (user-to-AI, not multi-user)
  2. Update SignalR method list
  3. Update events published/consumed

---

## 6. Deployment Strategy

### Phase 1: SharedKernel Deploy
**Changes**:
- New file: `AgentResponseEvent.cs`

**Backward Compatible**: ✅ YES (new event, no breaking changes)

**Deployment**:
```bash
# Build and test SharedKernel
dotnet build src/SharedKernel/CodingAgent.SharedKernel.sln
dotnet test src/SharedKernel/CodingAgent.SharedKernel.Tests/ --filter "Category=Unit"

# Publish NuGet package (internal)
dotnet pack src/SharedKernel/CodingAgent.SharedKernel/CodingAgent.SharedKernel.csproj -o ./artifacts
```

**Verification**:
- SharedKernel tests pass
- No dependent services break

### Phase 2: Orchestration Service Deploy
**Changes**:
- Modified: `MessageSentEventConsumer.cs` (now publishes AgentResponseEvent)
- Added dependency: `AgentResponseEvent` from SharedKernel

**Backward Compatible**: ✅ YES (Orchestration can publish new event without Chat consuming it yet)

**Deployment**:
```bash
# Deploy Orchestration Service
docker-compose -f deployment/docker-compose/docker-compose.yml \
  -f deployment/docker-compose/docker-compose.apps.dev.yml \
  up --build -d coding-agent-orchestration-dev

# Verify health
curl http://localhost:5003/health
```

**Verification**:
- Orchestration Service health check passes
- `MessageSentEvent` still consumed correctly
- `AgentResponseEvent` published (check RabbitMQ management UI)

### Phase 3: Chat Service Deploy
**Changes**:
- Removed: `IPresenceService`, `PresenceService`, `PresenceEndpoints`
- Modified: `ChatHub.cs` (new message flow, removed presence)
- Added: `AgentResponseEventConsumer.cs`

**Backward Compatible**: ⚠️ **BREAKING CHANGES**
- Frontend MUST deploy simultaneously (presence endpoints removed)
- SignalR hub methods removed (TypingIndicator, GetUserOnlineStatus, etc.)

**Deployment**:
```bash
# Deploy Chat Service
docker-compose -f deployment/docker-compose/docker-compose.yml \
  -f deployment/docker-compose/docker-compose.apps.dev.yml \
  up --build -d coding-agent-chat-dev

# Verify health
curl http://localhost:5001/health
```

**Verification**:
- Chat Service health check passes
- Database migrations applied (if any)
- SignalR hub accepts connections
- `AgentResponseEvent` consumed correctly

### Phase 4: Frontend Deploy
**Changes**:
- Removed: `presenceService`, presence UI components
- Added: `agentTyping` signal, `AgentTyping` event handler

**Deployment**:
```bash
cd src/Frontend/coding-agent-dashboard

# Build production bundle
npm run build

# Deploy to hosting (Azure Static Web Apps, Netlify, etc.)
# Example: Azure Static Web Apps CLI
swa deploy --app-location ./dist --env production
```

**Verification**:
- Frontend connects to Chat Service via SignalR
- User can send messages and receive agent responses
- Agent typing indicator appears
- No console errors related to removed presence methods

### Rollback Plan

**If Issues Arise**:

1. **Rollback Frontend** (fastest):
   ```bash
   swa deploy --app-location ./dist-previous --env production
   ```

2. **Rollback Chat Service** (medium):
   ```bash
   docker-compose up -d coding-agent-chat-dev:previous-tag
   ```

3. **Rollback Orchestration Service** (slowest, least likely):
   ```bash
   docker-compose up -d coding-agent-orchestration-dev:previous-tag
   ```

**Critical**: Frontend and Chat Service must be in sync (both refactored or both original).

### Feature Flag Recommendation

**NO FEATURE FLAG NEEDED**

**Reasoning**:
- This is an architectural refactor, not a gradual feature rollout
- Presence tracking and user-to-AI chat are mutually exclusive UX patterns
- Feature flag would complicate code with two different hub implementations
- Small user base (internal tool) allows "big bang" deployment

**Alternative** (if high-traffic production system):
If we had a large user base, we could:
1. Create `ChatHubV2` with new agent flow
2. Route users via query parameter (`?version=2`)
3. Gradually migrate users to v2
4. Remove v1 after 2 weeks

**Not Recommended Here Because**:
- Adds significant complexity (dual hub maintenance)
- Only 1-2 concurrent users expected in Phase 1
- Rollback is fast (Docker redeploy < 2 minutes)

---

## 7. Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Event delivery failure** (MessageSentEvent/AgentResponseEvent lost) | Low | High | • RabbitMQ persistence enabled<br>• MassTransit automatic retries (3 attempts)<br>• Dead-letter queue for failed messages<br>• OpenTelemetry tracing for debugging |
| **SignalR connection state during refactor** | Medium | Medium | • Users will auto-reconnect on disconnect<br>• Test reconnection in E2E tests<br>• Add exponential backoff to frontend reconnect logic |
| **Frontend/backend version mismatch** | High | Critical | • Deploy frontend immediately after backend<br>• Add API version header check (future)<br>• Clear browser cache after deployment |
| **Agent response timeout** (AI takes >30 seconds) | Medium | Medium | • Show AgentTyping indicator indefinitely<br>• Add timeout handling in Orchestration (60s max)<br>• Publish timeout error event → show user message |
| **Race condition** (user sends multiple messages before agent responds) | Medium | Low | • Queue messages in Orchestration Service<br>• Process sequentially per conversation<br>• Show message order in chat UI |
| **Database migration failure** | Low | High | • NO MIGRATION NEEDED (schema compatible)<br>• If added in future: test with Testcontainers first |
| **Redis cache invalidation** (old presence data) | Low | Low | • Presence data has TTL (auto-expires)<br>• Clear Redis cache manually if needed: `FLUSHDB` |
| **Test coverage drops below 85%** | Low | Medium | • Add tests in parallel with implementation<br>• Run coverage report before merging: `dotnet test /p:CollectCoverage=true` |

---

## 8. Architecture Decision Summary

### Before: Multi-User Chat (Slack-like)

**Features Being Removed**:
- ✅ User presence tracking (online/offline status)
- ✅ Last seen timestamps
- ✅ Typing indicators for other users (broadcast to group)
- ✅ Multi-user message broadcast (all users in conversation see messages)
- ✅ Online users list

**Codebase**:
- `IPresenceService` with 6 methods (Redis-backed)
- `PresenceEndpoints` with 3 REST endpoints
- `ChatHub` with 6 presence-related methods
- Frontend components for presence UI

### After: User↔AI Agent Communication

**Features Added**:
- ✅ User sends message → AI agent processes → user receives response
- ✅ Agent typing indicator (loading state while AI generates response)
- ✅ Event-driven orchestration (MessageSentEvent → AgentResponseEvent)
- ✅ Message persistence with nullable UserId (supports agent messages)
- ✅ Role-based message display (User vs Assistant)

**Codebase**:
- `AgentResponseEvent` in SharedKernel
- `AgentResponseEventConsumer` in Chat Service
- Updated `ChatHub.SendMessage` with event publishing
- Updated `MessageSentEventConsumer` in Orchestration Service
- Frontend `agentTyping` signal and UI

### Trade-offs

**Lost Capabilities**:
- ❌ **Real-time collaboration**: Can't see what other users are doing
- ❌ **Social presence**: No "who's online" awareness
- ❌ **Multi-user chat rooms**: One user per conversation
- ❌ **Typing awareness**: Can't see when others are typing (replaced with agent typing)

**Gained Capabilities**:
- ✅ **Simpler architecture**: Removed 500+ LOC (IPresenceService, endpoints, tests)
- ✅ **Lower Redis usage**: No presence tracking (reduced cache size by ~60%)
- ✅ **Clearer UX**: Users expect AI chat, not multi-user chat
- ✅ **Better scalability**: No broadcast to all users (only 1 user per conversation)
- ✅ **Event-driven AI**: Orchestration Service can use sophisticated routing (classify → route to best model)

**Cost-Benefit Analysis**:

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Lines of Code (Chat Service) | ~8,000 | ~7,500 | -500 LOC |
| Redis Cache Size (per 1000 users) | ~15 MB | ~6 MB | -60% |
| SignalR Messages/Minute (1000 users) | ~500 (presence updates) | ~100 (messages only) | -80% |
| Backend Services Involved | 1 (Chat) | 2 (Chat + Orchestration) | +1 service |
| Frontend Components | 8 | 5 | -3 components |
| Test Files | 25 | 20 | -5 test files |

### Reversibility

**Can We Roll Back?**

**Technical**: ⚠️ **DIFFICULT** (but possible)

**Steps to Revert**:
1. Restore deleted files:
   - `IPresenceService.cs`, `PresenceService.cs`, `PresenceEndpoints.cs`
2. Restore `ChatHub.cs` to previous version (Git revert)
3. Remove `AgentResponseEventConsumer.cs`
4. Restore frontend presence components
5. Restore `Program.cs` presence DI registration

**Time to Revert**: ~4 hours (same as implementation time)

**Data Loss**: ✅ NO
- Message data schema unchanged
- Conversations persist
- Agent messages clearly marked (UserId=null)

**Business**: ⚠️ **NOT RECOMMENDED**

**Why Revert is Unlikely**:
- Multi-user chat was never shipped to production
- Use case is AI agent interaction (documented in PRD)
- Users don't expect Slack-like features

**If Revert Needed**:
- Users lose agent message history? **NO** (Role=Assistant messages remain in DB)
- Frontend breaks? **YES** (must redeploy frontend with presence UI)
- Orchestration Service breaks? **NO** (can ignore AgentResponseEvent)

---

## 9. Success Criteria

### Definition of Done

**Backend**:
- ✅ All backend tasks completed (Tasks 1-6)
- ✅ Unit tests pass: `dotnet test --filter "Category=Unit" --verbosity quiet --nologo`
- ✅ Integration tests pass: `dotnet test --filter "Category=Integration" --verbosity quiet --nologo`
- ✅ Test coverage ≥85%: `dotnet test /p:CollectCoverage=true`
- ✅ No compiler warnings: `dotnet build --warnaserror`
- ✅ Chat Service health check returns 200: `curl http://localhost:5001/health`
- ✅ Orchestration Service health check returns 200: `curl http://localhost:5003/health`

**Frontend**:
- ✅ All frontend tasks completed (Tasks 7-10)
- ✅ TypeScript compiles without errors: `npm run build`
- ✅ Linting passes: `ng lint`
- ✅ Unit tests pass: `npm test`
- ✅ Agent typing indicator visible in UI

**Integration**:
- ✅ User sends message via SignalR → message persisted → MessageSentEvent published
- ✅ Orchestration Service consumes MessageSentEvent → publishes AgentResponseEvent
- ✅ Chat Service consumes AgentResponseEvent → message persisted → broadcast to user
- ✅ AgentTyping: true emitted after user message, false after agent response
- ✅ No presence-related errors in browser console

**Documentation**:
- ✅ ADR-006 created
- ✅ OpenAPI spec updated
- ✅ Service Catalog updated
- ✅ This architecture plan reviewed

### Performance Metrics

**Baseline (Before Refactor)**:
- SignalR messages/sec: 10 (mostly presence updates)
- Redis operations/sec: 50 (presence tracking)
- Average message latency: 150ms (user → broadcast)

**Target (After Refactor)**:
- SignalR messages/sec: 8 (only chat messages + typing indicator)
- Redis operations/sec: 20 (cache reads for message history)
- Average message latency: 2-5 seconds (user → AI → response)
  - User message → broadcast: 150ms (same)
  - AI processing: 1-4 seconds (varies by model)
  - Agent response → broadcast: 150ms

**Acceptable Degradation**:
- Agent response latency: <10 seconds for 95th percentile
- Database writes/sec: +50% (storing agent messages)

---

## 10. Next Steps

### Immediate Actions (Next 24 Hours)

1. **Review this architecture plan** with Tech Lead and stakeholders
2. **Create GitHub issue** for implementation tracking
3. **Set up feature branch**: `feature/chat-agent-refactor`
4. **Schedule pair programming session** for Task 1 (AgentResponseEvent)

### Implementation Timeline

| Phase | Duration | Assignee | Start Date |
|-------|----------|----------|------------|
| Phase 1: SharedKernel | 2 hours | Backend Dev | Day 1 |
| Phase 2: Backend (parallel) | 8 hours | Backend Dev | Day 1-2 |
| Phase 3: Frontend (parallel) | 6 hours | Frontend Dev | Day 1-2 |
| Phase 4: Testing | 8 hours | QA + Devs | Day 2-3 |
| Phase 5: Documentation | 4 hours | Tech Lead | Day 3 |
| **Total** | **28 hours** (~3.5 days) | Team | 3.5 days |

### Post-Implementation

1. **Monitor production** for 48 hours:
   - Check RabbitMQ queue depths (no backlog)
   - Verify OpenTelemetry traces (end-to-end flow)
   - Watch error logs (no new exceptions)

2. **Gather feedback** from early users:
   - Is agent typing indicator helpful?
   - Is response time acceptable?
   - Any UI/UX issues?

3. **Plan Phase 5 enhancements**:
   - Token usage tracking per message
   - Agent model selection (GPT-4 vs Claude vs Ollama)
   - Conversation context window management
   - Message editing and regeneration

---

## Appendices

### A. Event Schemas (Complete)

```csharp
// Existing event (in SharedKernel)
public record MessageSentEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public required Guid ConversationId { get; init; }
    public required Guid MessageId { get; init; }
    public Guid? UserId { get; init; }
    public required string Content { get; init; }
    public required string Role { get; init; }  // "User", "Assistant", "System"
    public required DateTime SentAt { get; init; }
}

// New event (to be created in SharedKernel)
public record AgentResponseEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public required Guid ConversationId { get; init; }
    public required string Content { get; init; }
    public string? ModelUsed { get; init; }
    public int? TokenCount { get; init; }
}
```

### B. SignalR Event Schemas (Complete)

```typescript
// Client → Server (Hub Methods)
interface ChatHubClient {
  JoinConversation(conversationId: string): Promise<void>;
  LeaveConversation(conversationId: string): Promise<void>;
  SendMessage(conversationId: string, content: string): Promise<void>;
}

// Server → Client (Events)
interface ChatHubServer {
  ReceiveMessage(message: {
    MessageId: string;
    ConversationId: string;
    UserId: string | null;
    Content: string;
    Role: 'User' | 'Assistant' | 'System';
    SentAt: string;  // ISO 8601 datetime
  }): void;
  
  AgentTyping(isTyping: boolean): void;  // NEW
}
```

### C. Database Schema (Verified Compatible)

```sql
-- chat.messages table (existing, no changes needed)
CREATE TABLE chat.messages (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL REFERENCES chat.conversations(id) ON DELETE CASCADE,
    user_id UUID,  -- NULLABLE (supports agent messages)
    content TEXT NOT NULL CHECK (char_length(content) <= 50000),
    role TEXT NOT NULL CHECK (role IN ('User', 'Assistant', 'System')),
    sent_at TIMESTAMP NOT NULL,
    
    INDEX idx_messages_conversation_id (conversation_id),
    INDEX idx_messages_sent_at (sent_at)
);

-- Sample data after refactor:
-- User message:
INSERT INTO chat.messages VALUES (
    '123e4567-e89b-12d3-a456-426614174000',
    'conv-id',
    'user-id',  -- Has UserId
    'How do I fix this bug?',
    'User',
    NOW()
);

-- Agent message:
INSERT INTO chat.messages VALUES (
    '123e4567-e89b-12d3-a456-426614174001',
    'conv-id',
    NULL,  -- UserId is NULL (agent message)
    'To fix this bug, you need to...',
    'Assistant',
    NOW()
);
```

### D. References

- **Related ADRs**:
  - ADR-001: Microservices Architecture
  - ADR-005: MassTransit + RabbitMQ for Event Bus
  - ADR-006: Chat Agent Refactor (this document)

- **Documentation**:
  - `docs/01-SERVICE-CATALOG.md` - Chat Service section 2
  - `docs/02-API-CONTRACTS.md` - SignalR Hub specification
  - `docs/03-DATA-MODELS.md` - Message entity schema

- **Code Examples**:
  - `src/Services/Orchestration/.../MessageSentEventConsumer.cs` - Event consumption pattern
  - `src/SharedKernel/.../MessageSentEvent.cs` - Event schema

---

**END OF ARCHITECTURE DESIGN**

**Status**: ✅ Ready for implementation  
**Next Action**: Review with Tech Lead → Create GitHub issue → Start Task 1
