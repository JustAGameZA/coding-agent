using System.Text.Json.Serialization;

namespace CodingAgent.Services.GitHub.Domain.Webhooks;

/// <summary>
/// GitHub push webhook payload.
/// </summary>
public class PushWebhookPayload : GitHubWebhookPayload
{
    /// <summary>
    /// Gets or sets the reference that was pushed (e.g., "refs/heads/main").
    /// </summary>
    [JsonPropertyName("ref")]
    public required string Ref { get; set; }

    /// <summary>
    /// Gets or sets the SHA of the most recent commit.
    /// </summary>
    [JsonPropertyName("after")]
    public required string After { get; set; }

    /// <summary>
    /// Gets or sets the head commit.
    /// </summary>
    [JsonPropertyName("head_commit")]
    public CommitInfo? HeadCommit { get; set; }

    /// <summary>
    /// Gets the branch name from the ref.
    /// </summary>
    public string GetBranchName()
    {
        if (Ref.StartsWith("refs/heads/"))
        {
            return Ref[11..];
        }
        return Ref;
    }
}

/// <summary>
/// Commit information from webhook payload.
/// </summary>
public class CommitInfo
{
    /// <summary>
    /// Gets or sets the commit ID/SHA.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the commit message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the commit timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the commit URL.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets the author information.
    /// </summary>
    [JsonPropertyName("author")]
    public required AuthorInfo Author { get; set; }
}

/// <summary>
/// Author information from commit.
/// </summary>
public class AuthorInfo
{
    /// <summary>
    /// Gets or sets the author name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the author email.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the author username.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}
