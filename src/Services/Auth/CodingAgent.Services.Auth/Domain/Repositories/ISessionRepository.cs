using CodingAgent.Services.Auth.Domain.Entities;

namespace CodingAgent.Services.Auth.Domain.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Session?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default);
    Task<Session> CreateAsync(Session session, CancellationToken ct = default);
    Task UpdateAsync(Session session, CancellationToken ct = default);
    Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken ct = default);
    Task DeleteExpiredSessionsAsync(CancellationToken ct = default);
}
