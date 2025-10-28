using CodingAgent.Services.Auth.Application.DTOs;

namespace CodingAgent.Services.Auth.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}
