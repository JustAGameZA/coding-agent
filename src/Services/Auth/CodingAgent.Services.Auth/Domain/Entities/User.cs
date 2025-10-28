namespace CodingAgent.Services.Auth.Domain.Entities;

/// <summary>
/// User entity representing an authenticated user in the system
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Roles { get; private set; } = "User"; // Comma-separated roles
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    private readonly List<Session> _sessions = new();
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    private readonly List<ApiKey> _apiKeys = new();
    public IReadOnlyCollection<ApiKey> ApiKeys => _apiKeys.AsReadOnly();

    // EF Core constructor
    private User() { }

    public User(string username, string email, string passwordHash, string roles = "User")
    {
        Id = Guid.NewGuid();
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Roles = roles;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRoles(string roles)
    {
        Roles = roles;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSession(Session session)
    {
        _sessions.Add(session);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddApiKey(ApiKey apiKey)
    {
        _apiKeys.Add(apiKey);
        UpdatedAt = DateTime.UtcNow;
    }

    public string[] GetRoles() => Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
