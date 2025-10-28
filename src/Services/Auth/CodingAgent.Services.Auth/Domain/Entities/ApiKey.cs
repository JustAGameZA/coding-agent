namespace CodingAgent.Services.Auth.Domain.Entities;

/// <summary>
/// API Key entity for programmatic access
/// </summary>
public class ApiKey
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string KeyHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    // EF Core constructor
    private ApiKey() { }

    public ApiKey(Guid userId, string keyHash, string name, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        KeyHash = keyHash;
        Name = name;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}
