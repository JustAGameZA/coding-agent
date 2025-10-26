using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating CICDMonitorDbContext instances during migrations.
/// </summary>
public class CICDMonitorDbContextFactory : IDesignTimeDbContextFactory<CICDMonitorDbContext>
{
    public CICDMonitorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CICDMonitorDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=coding_agent_cicd;Username=postgres;Password=postgres");

        return new CICDMonitorDbContext(optionsBuilder.Options);
    }
}
