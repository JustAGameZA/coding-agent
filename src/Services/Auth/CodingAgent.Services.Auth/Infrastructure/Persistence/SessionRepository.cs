using CodingAgent.Services.Auth.Domain.Entities;
using CodingAgent.Services.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Auth.Infrastructure.Persistence;

public class SessionRepository : ISessionRepository
{
    private readonly AuthDbContext _context;
    private readonly ILogger<SessionRepository> _logger;

    public SessionRepository(AuthDbContext context, ILogger<SessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Session?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default)
    {
        return await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && !s.IsRevoked, ct);
    }

    public async Task<Session> CreateAsync(Session session, CancellationToken ct = default)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, session.UserId);
        
        return session;
    }

    public async Task UpdateAsync(Session session, CancellationToken ct = default)
    {
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogDebug("Updated session {SessionId}", session.Id);
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(ct);

        foreach (var session in sessions)
        {
            session.Revoke();
        }

        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Revoked {Count} sessions for user {UserId}", sessions.Count, userId);
    }

    public async Task DeleteExpiredSessionsAsync(CancellationToken ct = default)
    {
        var expiredSessions = await _context.Sessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow || s.IsRevoked)
            .ToListAsync(ct);

        _context.Sessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Deleted {Count} expired sessions", expiredSessions.Count);
    }
}
