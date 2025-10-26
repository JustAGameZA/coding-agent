using System.Text.Json.Serialization;

namespace CodingAgent.Services.GitHub.Domain.Webhooks;

/// <summary>
/// GitHub pull request webhook payload.
/// </summary>
public class PullRequestWebhookPayload : GitHubWebhookPayload
{
    /// <summary>
    /// Gets or sets the pull request number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the pull request details.
    /// </summary>
    [JsonPropertyName("pull_request")]
    public required PullRequestDetails PullRequest { get; set; }
}

/// <summary>
/// Pull request details from webhook payload.
/// </summary>
public class PullRequestDetails
{
    /// <summary>
    /// Gets or sets the pull request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the pull request number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the pull request title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the pull request state (open, closed).
    /// </summary>
    [JsonPropertyName("state")]
    public required string State { get; set; }

    /// <summary>
    /// Gets or sets the pull request URL.
    /// </summary>
    [JsonPropertyName("html_url")]
    public required string HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets the user who created the pull request.
    /// </summary>
    [JsonPropertyName("user")]
    public required UserInfo User { get; set; }

    /// <summary>
    /// Gets or sets whether the pull request was merged.
    /// </summary>
    [JsonPropertyName("merged")]
    public bool Merged { get; set; }

    /// <summary>
    /// Gets or sets the merged_at timestamp.
    /// </summary>
    [JsonPropertyName("merged_at")]
    public DateTime? MergedAt { get; set; }
}
