using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CodingAgent.Services.Auth.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CodingAgent.Services.Auth.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        // Prefer Authentication:Jwt when provided, else fall back to Jwt
        var authJwt = _configuration.GetSection("Authentication:Jwt");
        string? secretKey;
        string? issuer;
        string? audience;

        if (!string.IsNullOrWhiteSpace(authJwt["SecretKey"]))
        {
            secretKey = authJwt["SecretKey"]!;
            issuer = !string.IsNullOrWhiteSpace(authJwt["Issuer"]) ? authJwt["Issuer"] : "CodingAgent";
            audience = !string.IsNullOrWhiteSpace(authJwt["Audience"]) ? authJwt["Audience"] : "CodingAgent.API";
        }
        else
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured");
            issuer = !string.IsNullOrWhiteSpace(jwtSettings["Issuer"]) ? jwtSettings["Issuer"] : "CodingAgent";
            audience = !string.IsNullOrWhiteSpace(jwtSettings["Audience"]) ? jwtSettings["Audience"] : "CodingAgent.API";
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("uid", user.Id.ToString()),
        };

        // Add roles as claims
        foreach (var role in user.GetRoles())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15), // 15 minutes
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
