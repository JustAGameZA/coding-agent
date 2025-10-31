using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CodingAgent.Services.Chat.Tests.Integration;

public sealed class ChatServiceFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    public HttpClient Client { get; private set; } = default!;
    public WebApplicationFactory<Program> Factory { get; private set; } = default!;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ChatServiceFixture() { }

    public async Task InitializeAsync()
    {
        string? connectionString = null;

        // Try to start PostgreSQL Testcontainer; fall back to in-memory if Docker is unavailable
        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithDatabase("chat_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithImage("postgres:16-alpine")
                .Build();

            await _postgres.StartAsync();
            connectionString = _postgres.GetConnectionString();
        }
        catch (ArgumentException)
        {
            // Docker endpoint not detected; continue with in-memory database
            connectionString = null;
        }

        // Generate a test JWT secret for test authentication
        var testJwtSecret = "TestSecretKeyForDevelopment12345678901234567890"; // Must be >= 32 chars
        var testJwtToken = GenerateTestJwtToken(_testUserId, testJwtSecret);

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Ensure non-production environment for tests so the app uses in-memory DB
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    var dict = new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = testJwtSecret,
                        ["Jwt:Issuer"] = "CodingAgent",
                        ["Jwt:Audience"] = "CodingAgent.API"
                    };
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        dict["ConnectionStrings:ChatDb"] = connectionString;
                    }
                    config.AddInMemoryCollection(dict!);
                });
            });

        Client = Factory.CreateClient();
        // Add JWT token to all requests
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", testJwtToken);
    }

    private static string GenerateTestJwtToken(Guid userId, string secret)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: "CodingAgent",
            audience: "CodingAgent.API",
            claims: new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("nameid", userId.ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        if (_postgres is not null)
        {
            try
            { await _postgres.StopAsync(); }
            catch { /* ignore */ }
            try
            { await _postgres.DisposeAsync(); }
            catch { /* ignore */ }
        }
    }
}

[CollectionDefinition("ChatServiceCollection")]
public sealed class ChatServiceCollection : ICollectionFixture<ChatServiceFixture>
{
}
