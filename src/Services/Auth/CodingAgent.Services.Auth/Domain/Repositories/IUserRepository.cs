using CodingAgent.Services.Auth.Domain.Entities;

namespace CodingAgent.Services.Auth.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default);
    Task<(List<User> Users, int TotalCount)> GetPagedAsync(int page, int pageSize, string? searchQuery = null, string? roleFilter = null, CancellationToken ct = default);
}
