using System.Diagnostics;
using System.Text.Json;

namespace CodingAgent.Services.Dashboard.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for Chat Service
/// </summary>
public class ChatServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatServiceClient> _logger;
    private readonly ActivitySource _activitySource;

    public ChatServiceClient(HttpClient httpClient, ILogger<ChatServiceClient> logger, ActivitySource activitySource)
    {
        _httpClient = httpClient;
        _logger = logger;
        _activitySource = activitySource;
    }

    public virtual async Task<ChatStatsDto?> GetStatsAsync(CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("ChatServiceClient.GetStats");
        
        try
        {
            _logger.LogInformation("Fetching stats from Chat Service");
            var response = await _httpClient.GetAsync("/conversations", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Chat Service returned {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var conversations = JsonSerializer.Deserialize<List<ConversationDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (conversations == null)
            {
                return null;
            }

            // Estimate messages count (simplified - would need separate endpoint in real scenario)
            var totalMessages = conversations.Count * 5; // Average estimate

            return new ChatStatsDto
            {
                TotalConversations = conversations.Count,
                TotalMessages = totalMessages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Chat Service stats");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return null;
        }
    }
}

public record ChatStatsDto
{
    public int TotalConversations { get; init; }
    public int TotalMessages { get; init; }
}

public record ConversationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
