using System.Text.Json.Serialization;

namespace CodingAgent.Services.GitHub.Domain.Webhooks;

/// <summary>
/// Base class for all GitHub webhook payloads.
/// </summary>
public abstract class GitHubWebhookPayload
{
    /// <summary>
    /// Gets or sets the action performed (e.g., "opened", "closed", "synchronize").
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the repository information.
    /// </summary>
    [JsonPropertyName("repository")]
    public required RepositoryInfo Repository { get; set; }

    /// <summary>
    /// Gets or sets the sender information.
    /// </summary>
    [JsonPropertyName("sender")]
    public required UserInfo Sender { get; set; }
}

/// <summary>
/// Repository information from webhook payload.
/// </summary>
public class RepositoryInfo
{
    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the repository full name (owner/repo).
    /// </summary>
    [JsonPropertyName("full_name")]
    public required string FullName { get; set; }

    /// <summary>
    /// Gets or sets the repository owner.
    /// </summary>
    [JsonPropertyName("owner")]
    public required UserInfo Owner { get; set; }
}

/// <summary>
/// User information from webhook payload.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Gets or sets the user login.
    /// </summary>
    [JsonPropertyName("login")]
    public required string Login { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
