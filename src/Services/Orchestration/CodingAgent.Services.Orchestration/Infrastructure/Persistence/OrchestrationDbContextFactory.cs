using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodingAgent.Services.Orchestration.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for OrchestrationDbContext to enable EF Core migrations
/// </summary>
public class OrchestrationDbContextFactory : IDesignTimeDbContextFactory<OrchestrationDbContext>
{
    public OrchestrationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrchestrationDbContext>();

        // Use PostgreSQL for migrations
        // Connection string will be provided through environment variable or appsettings
        optionsBuilder.UseNpgsql("Host=localhost;Database=coding_agent;Username=postgres;Password=postgres");

        return new OrchestrationDbContext(optionsBuilder.Options);
    }
}
