using System.Security.Cryptography;
using System.Text;
using CodingAgent.Services.Auth.Application.DTOs;
using CodingAgent.Services.Auth.Domain.Entities;
using CodingAgent.Services.Auth.Domain.Repositories;
using CodingAgent.Services.Auth.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for user: {Username} from IP: {IpAddress}", request.Username, ipAddress);

        // Find user by username
        var user = await _userRepository.GetByUsernameAsync(request.Username, ct);
        
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found - {Username}", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User is deactivated - {Username}", request.Username);
            throw new UnauthorizedAccessException("Account is deactivated");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user - {Username}", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Generate tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashRefreshToken(refreshToken);

        // Create session
        var session = new Session(
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(7), // 7 days expiry
            ipAddress,
            userAgent
        );

        user.AddSession(session);
        await _sessionRepository.CreateAsync(session, ct);
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("Login successful for user: {Username}", request.Username);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: 900, // 15 minutes in seconds
            TokenType: "Bearer",
            User: new UserDto(
                user.Id,
                user.Username,
                user.Email,
                user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                user.CreatedAt
            )
        );
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        _logger.LogInformation("Registration attempt for username: {Username}, email: {Email}", request.Username, request.Email);

        // Check if user already exists
        if (await _userRepository.ExistsAsync(request.Username, request.Email, ct))
        {
            _logger.LogWarning("Registration failed: Username or email already exists - {Username}, {Email}", 
                request.Username, request.Email);
            throw new InvalidOperationException("Username or email already exists");
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = new User(request.Username, request.Email, passwordHash);
        await _userRepository.CreateAsync(user, ct);

        // Generate tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashRefreshToken(refreshToken);

        // Create session
        var session = new Session(
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(7),
            ipAddress,
            userAgent
        );

        user.AddSession(session);
        await _sessionRepository.CreateAsync(session, ct);
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("Registration successful for user: {Username}", request.Username);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: 900,
            TokenType: "Bearer",
            User: new UserDto(
                user.Id,
                user.Username,
                user.Email,
                user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                user.CreatedAt
            )
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        _logger.LogInformation("Token refresh attempt from IP: {IpAddress}", ipAddress);

        var refreshTokenHash = HashRefreshToken(request.RefreshToken);

        // Find session by refresh token hash
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(refreshTokenHash, ct);
        
        if (session == null || !session.IsValid())
        {
            _logger.LogWarning("Token refresh failed: Invalid or expired refresh token");
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(session.UserId, ct);
        
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Token refresh failed: User not found or deactivated - UserId: {UserId}", session.UserId);
            throw new UnauthorizedAccessException("User not found or deactivated");
        }

        // Revoke old session (refresh token rotation)
        session.Revoke();
        await _sessionRepository.UpdateAsync(session, ct);

        // Generate new tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();
        var newRefreshTokenHash = HashRefreshToken(newRefreshToken);

        // Create new session
        var newSession = new Session(
            user.Id,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(7),
            ipAddress,
            userAgent
        );

        user.AddSession(newSession);
        await _sessionRepository.CreateAsync(newSession, ct);
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("Token refresh successful for user: {UserId}", user.Id);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresIn: 900,
            TokenType: "Bearer",
            User: new UserDto(
                user.Id,
                user.Username,
                user.Email,
                user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                user.CreatedAt
            )
        );
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        
        if (user == null)
        {
            return null;
        }

        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.GetRoles(),
            user.CreatedAt
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(refreshTokenHash, ct);
        
        if (session != null)
        {
            session.Revoke();
            await _sessionRepository.UpdateAsync(session, ct);
            _logger.LogInformation("Refresh token revoked for session: {SessionId}", session.Id);
        }
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(newPasswordHash);
        await _userRepository.UpdateAsync(user, ct);

        // Revoke all existing sessions
        await _sessionRepository.RevokeAllUserSessionsAsync(userId, ct);

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashBytes);
    }
}
