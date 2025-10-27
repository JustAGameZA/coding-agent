using System.Net;
using System.Net.Http.Json;
using CodingAgent.Services.Auth.Application.DTOs;
using CodingAgent.Services.Auth.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace CodingAgent.Services.Auth.Tests.Integration;

[Trait("Category", "Integration")]
public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public AuthEndpointsTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("coding_agent_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    services.RemoveAll<DbContextOptions<AuthDbContext>>();
                    services.RemoveAll<AuthDbContext>();

                    // Add PostgreSQL DbContext with Testcontainers
                    services.AddDbContext<AuthDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });

                    // Ensure database is created and migrated
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                    db.Database.Migrate();
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new RegisterRequest(
            Username: "testuser",
            Email: "test@example.com",
            Password: "Password123!",
            ConfirmPassword: "Password123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldReturnBadRequest()
    {
        // Arrange
        var request1 = new RegisterRequest("duplicateuser", "user1@example.com", "Password123!", "Password123!");
        var request2 = new RegisterRequest("duplicateuser", "user2@example.com", "Password123!", "Password123!");

        // Act
        await _client!.PostAsJsonAsync("/auth/register", request1);
        var response = await _client.PostAsJsonAsync("/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk()
    {
        // Arrange - Register a user first
        var registerRequest = new RegisterRequest("loginuser", "login@example.com", "Password123!", "Password123!");
        await _client!.PostAsJsonAsync("/auth/register", registerRequest);

        var loginRequest = new LoginRequest("loginuser", "Password123!");

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent", "WrongPassword!");

        // Act
        var response = await _client!.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange - Register and login to get a refresh token
        var registerRequest = new RegisterRequest("refreshuser", "refresh@example.com", "Password123!", "Password123!");
        var registerResponse = await _client!.PostAsJsonAsync("/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRequest = new RefreshTokenRequest(authResponse!.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(authResponse.RefreshToken); // Token rotation
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest("invalid_refresh_token");

        // Act
        var response = await _client!.PostAsJsonAsync("/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ShouldReturnUserInfo()
    {
        // Arrange - Register to get a token
        var registerRequest = new RegisterRequest("meuser", "me@example.com", "Password123!", "Password123!");
        var registerResponse = await _client!.PostAsJsonAsync("/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.GetAsync("/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        result.Should().NotBeNull();
        result!.Username.Should().Be("meuser");
        result.Email.Should().Be("me@example.com");
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetMe_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client!.GetAsync("/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_AfterUsingOnce_ShouldInvalidateOldToken()
    {
        // Arrange - Register and get initial refresh token
        var registerRequest = new RegisterRequest("rotationuser", "rotation@example.com", "Password123!", "Password123!");
        var registerResponse = await _client!.PostAsJsonAsync("/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var firstRefreshRequest = new RefreshTokenRequest(authResponse!.RefreshToken);
        
        // Act - Use refresh token once
        var firstRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", firstRefreshRequest);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use the old refresh token again
        var secondRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", firstRefreshRequest);

        // Assert - Old token should be invalid
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
