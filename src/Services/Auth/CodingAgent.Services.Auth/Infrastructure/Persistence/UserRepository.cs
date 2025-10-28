using CodingAgent.Services.Auth.Domain.Entities;
using CodingAgent.Services.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Auth.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(AuthDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.Sessions.Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow).OrderByDescending(s => s.CreatedAt).Take(5))
            .Include(u => u.ApiKeys.Where(a => !a.IsRevoked && a.ExpiresAt > DateTime.UtcNow).OrderByDescending(a => a.CreatedAt).Take(5))
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created user {UserId} - {Username}", user.Id, user.Username);
        
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogDebug("Updated user {UserId}", user.Id);
    }

    public async Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Username == username || u.Email == email, ct);
    }
}
