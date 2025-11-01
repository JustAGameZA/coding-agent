using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingAgent.Services.Orchestration.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;

/// <summary>
/// DTO for conversation messages from Chat Service
/// </summary>
public record ConversationMessageDto
{
    public Guid Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
}

/// <summary>
/// Response DTO from Chat Service messages endpoint
/// </summary>
internal record PagedMessagesResponse
{
    public List<MessageDto> Items { get; init; } = new();
    public string? NextCursor { get; init; }
}

internal record MessageDto
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
    public List<AttachmentDto> Attachments { get; init; } = new();
}

internal record AttachmentDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string StorageUrl { get; init; } = string.Empty;
}

/// <summary>
/// HTTP client for Chat Service.
/// Handles posting AI agent responses back to chat conversations.
/// </summary>
public class ChatServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatServiceClient> _logger;
    private readonly ActivitySource _activitySource;
    private readonly ServiceTokenGenerator _tokenGenerator;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ChatServiceClient(
        HttpClient httpClient,
        ILogger<ChatServiceClient> logger,
        ActivitySource activitySource,
        ServiceTokenGenerator tokenGenerator)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        _tokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
    }

    /// <summary>
    /// Gets conversation history for context (service-to-service call).
    /// </summary>
    public async Task<IEnumerable<ConversationMessageDto>> GetConversationHistoryAsync(
        Guid conversationId,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ChatServiceClient.GetConversationHistory");
        activity?.SetTag("conversation.id", conversationId);
        activity?.SetTag("max.messages", maxMessages);

        try
        {
            _logger.LogInformation(
                "Getting conversation history for conversation {ConversationId} (max {MaxMessages} messages)",
                conversationId, maxMessages);

            // Generate service token for authentication
            var serviceToken = _tokenGenerator.GenerateServiceToken();
            
            // Create request message to add authorization header
            // Use internal service endpoint that bypasses user ownership check
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get, 
                $"/conversations/{conversationId}/messages/history?limit={maxMessages}");
            
            // Add JWT token if available
            if (!string.IsNullOrEmpty(serviceToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
                _logger.LogDebug("Added JWT Bearer token to Chat Service request");
            }
            
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Conversation {ConversationId} not found", conversationId);
                return Enumerable.Empty<ConversationMessageDto>();
            }
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<PagedMessagesResponse>(JsonOptions, cancellationToken);
            
            // Convert to simple DTO format
            return result?.Items.Select(m => new ConversationMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                SentAt = m.SentAt
            }) ?? Enumerable.Empty<ConversationMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get conversation history for {ConversationId}, continuing without context", conversationId);
            // Don't fail the entire request if we can't get history - just continue without context
            return Enumerable.Empty<ConversationMessageDto>();
        }
    }

    /// <summary>
    /// Posts an AI agent response to a chat conversation.
    /// </summary>
    public async Task PostAgentResponseAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ChatServiceClient.PostAgentResponse");
        activity?.SetTag("conversation.id", conversationId);
        activity?.SetTag("content.length", content.Length);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Posting agent response to conversation {ConversationId}",
                conversationId);

            var request = new { Content = content };
            
            // Generate service token for authentication
            var serviceToken = _tokenGenerator.GenerateServiceToken();
            
            // Create request message to add authorization header
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/conversations/{conversationId}/agent-response");
            requestMessage.Content = JsonContent.Create(request, options: JsonOptions);
            
            // Add JWT token if available
            if (!string.IsNullOrEmpty(serviceToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
                _logger.LogDebug("Added JWT Bearer token to Chat Service request");
            }
            else
            {
                _logger.LogWarning("Service token is empty - request may be rejected if JWT is required");
            }
            
            // Use cancellation token to enforce timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            var response = await _httpClient.SendAsync(requestMessage, timeoutCts.Token);

            response.EnsureSuccessStatusCode();

            stopwatch.Stop();
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogInformation(
                "Agent response posted successfully to conversation {ConversationId} in {Duration}ms",
                conversationId,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Operation cancelled");
            _logger.LogWarning(
                "Post agent response operation cancelled for conversation {ConversationId} after {Duration}ms",
                conversationId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout or cancelled");
            var isTimeout = ex.InnerException is TimeoutException || 
                           (!cancellationToken.IsCancellationRequested && stopwatch.ElapsedMilliseconds >= (_httpClient.Timeout.TotalMilliseconds * 0.9));
            
            if (isTimeout)
            {
                _logger.LogWarning(
                    "Timeout posting agent response to conversation {ConversationId} after {Duration}ms (HttpClient timeout: {HttpTimeout}ms)",
                    conversationId,
                    stopwatch.ElapsedMilliseconds,
                    _httpClient.Timeout.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Request cancelled for conversation {ConversationId} after {Duration}ms",
                    conversationId,
                    stopwatch.ElapsedMilliseconds);
            }
            throw;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, 
                "Failed to post agent response to conversation {ConversationId} after {Duration}ms",
                conversationId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

