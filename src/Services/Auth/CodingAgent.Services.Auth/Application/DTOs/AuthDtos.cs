namespace CodingAgent.Services.Auth.Application.DTOs;

public record LoginRequest(
    string Username,
    string Password
);

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string[] Roles,
    DateTime CreatedAt
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);
