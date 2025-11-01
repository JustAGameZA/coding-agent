using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CodingAgent.Services.Orchestration.Infrastructure.Security;

/// <summary>
/// Generates JWT tokens for service-to-service authentication.
/// Used by Orchestration Service to authenticate with Chat Service.
/// </summary>
public class ServiceTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceTokenGenerator> _logger;

    public ServiceTokenGenerator(
        IConfiguration configuration,
        ILogger<ServiceTokenGenerator> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a JWT token for service-to-service authentication with the "orchestration" role.
    /// </summary>
    public string GenerateServiceToken()
    {
        // Get JWT configuration - try multiple paths (match Chat Service format)
        var jwtSecret = _configuration["Jwt:Secret"]
            ?? _configuration["Authentication:Jwt:SecretKey"]
            ?? _configuration["JWT_SECRET"];

        // Match Chat Service defaults: Issuer=http://localhost:5000, Audience=coding-agent-api
        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? _configuration["Authentication:Jwt:Issuer"]
            ?? "http://localhost:5000";

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? _configuration["Authentication:Jwt:Audience"]
            ?? "coding-agent-api";

        if (string.IsNullOrEmpty(jwtSecret))
        {
            _logger.LogWarning("JWT secret not configured - service-to-service authentication may fail. Checked paths: Jwt:Secret, Authentication:Jwt:SecretKey, JWT_SECRET");
            // Try to continue without token - Chat Service should allow anonymous in dev if JWT is not configured
            return string.Empty; // Return empty token if JWT is not configured
        }
        
        _logger.LogDebug("JWT secret found, generating service token. Issuer: {Issuer}, Audience: {Audience}", jwtIssuer, jwtAudience);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "orchestration-service"), // Service identifier
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "orchestration"), // Required role for Chat Service endpoints
            new Claim("service", "orchestration") // Additional service claim
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1), // 1 hour validity for service tokens
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        _logger.LogDebug("Generated service token for orchestration service");
        return tokenString;
    }
}

