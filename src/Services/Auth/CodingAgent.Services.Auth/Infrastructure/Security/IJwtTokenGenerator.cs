using CodingAgent.Services.Auth.Domain.Entities;

namespace CodingAgent.Services.Auth.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
}
