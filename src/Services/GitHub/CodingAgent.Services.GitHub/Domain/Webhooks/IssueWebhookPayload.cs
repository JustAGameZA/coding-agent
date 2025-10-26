using System.Text.Json.Serialization;

namespace CodingAgent.Services.GitHub.Domain.Webhooks;

/// <summary>
/// GitHub issue webhook payload.
/// </summary>
public class IssueWebhookPayload : GitHubWebhookPayload
{
    /// <summary>
    /// Gets or sets the issue details.
    /// </summary>
    [JsonPropertyName("issue")]
    public required IssueDetails Issue { get; set; }

    /// <summary>
    /// Gets or sets the comment details (only present for comment actions).
    /// </summary>
    [JsonPropertyName("comment")]
    public CommentDetails? Comment { get; set; }
}

/// <summary>
/// Issue details from webhook payload.
/// </summary>
public class IssueDetails
{
    /// <summary>
    /// Gets or sets the issue ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the issue number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the issue title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the issue state (open, closed).
    /// </summary>
    [JsonPropertyName("state")]
    public required string State { get; set; }

    /// <summary>
    /// Gets or sets the issue URL.
    /// </summary>
    [JsonPropertyName("html_url")]
    public required string HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets the user who created the issue.
    /// </summary>
    [JsonPropertyName("user")]
    public required UserInfo User { get; set; }

    /// <summary>
    /// Gets or sets the issue body.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

/// <summary>
/// Comment details from webhook payload.
/// </summary>
public class CommentDetails
{
    /// <summary>
    /// Gets or sets the comment ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the comment body.
    /// </summary>
    [JsonPropertyName("body")]
    public required string Body { get; set; }

    /// <summary>
    /// Gets or sets the user who created the comment.
    /// </summary>
    [JsonPropertyName("user")]
    public required UserInfo User { get; set; }
}
