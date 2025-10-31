using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodingAgent.Services.Auth.Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodingAgent.Services.Auth.Tests.Integration;

[Collection("AuthServiceCollection")]
[Trait("Category", "Integration")]
public class AdminEndpointsTests : IClassFixture<AuthServiceFixture>
{
    private readonly HttpClient _client;
    private readonly AuthServiceFixture _fixture;

    public AdminEndpointsTests(AuthServiceFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetUsers_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithUserRole_ShouldReturn403()
    {
        // Arrange - Create user and get token
        var registerRequest = new RegisterRequest(
            Username: $"testuser_{Guid.NewGuid():N}",
            Email: $"testuser_{Guid.NewGuid():N}@test.com",
            Password: "Test@1234!",
            ConfirmPassword: "Test@1234!"
        );

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.GetAsync("/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WithAdminRole_ShouldReturnUserList()
    {
        // Arrange - Create admin user
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/admin/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userList = await response.Content.ReadFromJsonAsync<UserListResponse>();
        userList.Should().NotBeNull();
        userList!.Users.Should().NotBeEmpty();
        userList.Page.Should().Be(1);
        userList.PageSize.Should().Be(10);
        userList.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetUsers_WithSearchFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var uniqueUsername = $"searchuser_{Guid.NewGuid():N}";
        await CreateUserAsync(uniqueUsername, $"{uniqueUsername}@test.com", "User");

        // Act
        var response = await _client.GetAsync($"/admin/users?search={uniqueUsername.Substring(0, 10)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userList = await response.Content.ReadFromJsonAsync<UserListResponse>();
        userList.Should().NotBeNull();
        userList!.Users.Should().Contain(u => u.Username == uniqueUsername);
    }

    [Fact]
    public async Task GetUsers_WithRoleFilter_ShouldReturnOnlyUsersWithRole()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/admin/users?role=Admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userList = await response.Content.ReadFromJsonAsync<UserListResponse>();
        userList.Should().NotBeNull();
        userList!.Users.Should().OnlyContain(u => u.Roles.Contains("Admin"));
    }

    [Fact]
    public async Task GetUsers_WithInvalidPageSize_ShouldReturn400()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/admin/users?pageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserById_WithAdminRole_ShouldReturnUserDetail()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var username = $"detailuser_{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(username, $"{username}@test.com", "User");

        // Act
        var response = await _client.GetAsync($"/admin/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDetail = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        userDetail.Should().NotBeNull();
        userDetail!.Id.Should().Be(userId);
        userDetail.Username.Should().Be(username);
        userDetail.SessionCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/admin/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUserRoles_WithValidRoles_ShouldSucceed()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var username = $"roleuser_{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(username, $"{username}@test.com", "User");

        var updateRequest = new UpdateUserRolesRequest(new[] { "User", "Admin" });

        // Act
        var response = await _client.PutAsJsonAsync($"/admin/users/{userId}/roles", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify roles were updated
        var getUserResponse = await _client.GetAsync($"/admin/users/{userId}");
        var userDetail = await getUserResponse.Content.ReadFromJsonAsync<UserDetailDto>();
        userDetail!.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task UpdateUserRoles_WithInvalidRole_ShouldReturn400()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var username = $"invalidrole_{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(username, $"{username}@test.com", "User");

        var updateRequest = new UpdateUserRolesRequest(new[] { "InvalidRole" });

        // Act
        var response = await _client.PutAsJsonAsync($"/admin/users/{userId}/roles", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUserRoles_WithEmptyRoles_ShouldReturn400()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var username = $"emptyrole_{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(username, $"{username}@test.com", "User");

        var updateRequest = new UpdateUserRolesRequest(Array.Empty<string>());

        // Act
        var response = await _client.PutAsJsonAsync($"/admin/users/{userId}/roles", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUserRoles_RemovingOwnAdminRole_ShouldReturn400()
    {
        // Arrange
        var (adminId, adminToken) = await CreateAdminUserAndGetTokenWithIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var updateRequest = new UpdateUserRolesRequest(new[] { "User" });

        // Act
        var response = await _client.PutAsJsonAsync($"/admin/users/{adminId}/roles", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ActivateUser_WithDeactivatedUser_ShouldSucceed()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var username = $"activateuser_{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(username, $"{username}@test.com", "User");

        // Deactivate first
        await _client.PutAsync($"/admin/users/{userId}/deactivate", null);

        // Act
        var response = await _client.PutAsync($"/admin/users/{userId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify user is active
        var getUserResponse = await _client.GetAsync($"/admin/users/{userId}");
        var userDetail = await getUserResponse.Content.ReadFromJsonAsync<UserDetailDto>();
        userDetail!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateUser_WithActiveUser_ShouldSucceed()
    {
        // Arrange
        var adminToken = await CreateAdminUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var username = $"deactivateuser_{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(username, $"{username}@test.com", "User");

        // Act
        var response = await _client.PutAsync($"/admin/users/{userId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify user is inactive
        var getUserResponse = await _client.GetAsync($"/admin/users/{userId}");
        var userDetail = await getUserResponse.Content.ReadFromJsonAsync<UserDetailDto>();
        userDetail!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUser_DeactivatingOwnAccount_ShouldReturn400()
    {
        // Arrange
        var (adminId, adminToken) = await CreateAdminUserAndGetTokenWithIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.PutAsync($"/admin/users/{adminId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Helper methods
    private async Task<string> CreateAdminUserAndGetTokenAsync()
    {
        var (_, token) = await CreateAdminUserAndGetTokenWithIdAsync();
        return token;
    }

    private async Task<(Guid UserId, string Token)> CreateAdminUserAndGetTokenWithIdAsync()
    {
        var username = $"admin_{Guid.NewGuid():N}";
        var email = $"{username}@test.com";

        var registerRequest = new RegisterRequest(
            Username: username,
            Email: email,
            Password: "Admin@1234!",
            ConfirmPassword: "Admin@1234!"
        );

        var response = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        var userId = authResponse!.User.Id;

        // Manually update user to have Admin role (since registration creates User role only)
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CodingAgent.Services.Auth.Infrastructure.Persistence.AuthDbContext>();
        var user = await dbContext.Users.FindAsync(userId);
        user!.UpdateRoles("Admin,User");
        await dbContext.SaveChangesAsync();

        // Get new token with admin role
        var loginRequest = new LoginRequest(username, "Admin@1234!");
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginAuthResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return (userId, loginAuthResponse!.AccessToken);
    }

    private async Task<Guid> CreateUserAsync(string username, string email, string roles)
    {
        var registerRequest = new RegisterRequest(
            Username: username,
            Email: email,
            Password: "Test@1234!",
            ConfirmPassword: "Test@1234!"
        );

        var response = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        var userId = authResponse!.User.Id;

        // Update roles if needed
        if (roles != "User")
        {
            using var scope = _fixture.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CodingAgent.Services.Auth.Infrastructure.Persistence.AuthDbContext>();
            var user = await dbContext.Users.FindAsync(userId);
            user!.UpdateRoles(roles);
            await dbContext.SaveChangesAsync();
        }

        return userId;
    }
}
