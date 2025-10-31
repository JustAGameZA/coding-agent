using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CodingAgent.Services.Auth.Tests.Integration;

public class AuthServiceFixture : WebApplicationFactory<Program>
{
    public IServiceScope CreateScope() => Services.CreateScope();
}


