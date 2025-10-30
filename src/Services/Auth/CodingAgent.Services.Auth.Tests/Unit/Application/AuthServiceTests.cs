using CodingAgent.Services.Auth.Application.DTOs;
using CodingAgent.Services.Auth.Application.Services;
using CodingAgent.Services.Auth.Domain.Entities;
using CodingAgent.Services.Auth.Domain.Repositories;
using CodingAgent.Services.Auth.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodingAgent.Services.Auth.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new LoginRequest("testuser", "password123");
        var user = new User("testuser", "test@example.com", "hashed_password");
        
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        
        _jwtTokenGeneratorMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");
        
        _sessionRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session s, CancellationToken ct) => s);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().Be(900);
        
        _sessionRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest("nonexistent", "password123");
        
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.LoginAsync(request, "127.0.0.1", "test-agent"));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest("testuser", "wrong_password");
        var user = new User("testuser", "test@example.com", "hashed_password");
        
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.LoginAsync(request, "127.0.0.1", "test-agent"));
    }

    [Fact]
    public async Task LoginAsync_WithDeactivatedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest("testuser", "password123");
        var user = new User("testuser", "test@example.com", "hashed_password");
        user.Deactivate();
        
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.LoginAsync(request, "127.0.0.1", "test-agent"));
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@example.com", "Password123!", "Password123!");
        
        _userRepositoryMock.Setup(x => x.ExistsAsync(request.Username, request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _passwordHasherMock.Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_password");
        
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);
        
        _jwtTokenGeneratorMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");
        
        _sessionRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session s, CancellationToken ct) => s);

        // Act
        var result = await _authService.RegisterAsync(request, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        
        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest("existinguser", "new@example.com", "Password123!", "Password123!");
        
        _userRepositoryMock.Setup(x => x.ExistsAsync(request.Username, request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _authService.RegisterAsync(request, "127.0.0.1", "test-agent"));
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithValidUserId_ShouldReturnUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("testuser", "test@example.com", "hashed_password");
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("testuser", "test@example.com", "old_hashed_password");
        var request = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", "NewPassword123!");
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            .Returns(true);
        
        _passwordHasherMock.Setup(x => x.HashPassword(request.NewPassword))
            .Returns("new_hashed_password");

        // Act
        await _authService.ChangePasswordAsync(userId, request);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepositoryMock.Verify(x => x.RevokeAllUserSessionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("testuser", "test@example.com", "old_hashed_password");
        var request = new ChangePasswordRequest("WrongPassword!", "NewPassword123!", "NewPassword123!");
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.ChangePasswordAsync(userId, request));
    }
}
