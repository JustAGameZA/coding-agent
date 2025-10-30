namespace CodingAgent.Services.Auth.Domain.Entities;

/// <summary>
/// Session entity representing a user's refresh token session
/// </summary>
public class Session
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    // EF Core constructor
    private Session() { }

    public Session(Guid userId, string refreshTokenHash, DateTime expiresAt, string ipAddress, string userAgent)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }
}
