using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating CICDMonitorDbContext instances during migrations.
/// Loads configuration from appsettings and environment variables to avoid hardcoded credentials.
/// </summary>
public class CICDMonitorDbContextFactory : IDesignTimeDbContextFactory<CICDMonitorDbContext>
{
    public CICDMonitorDbContext CreateDbContext(string[] args)
    {
        // Resolve environment (default to Development for local tooling)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Build configuration from appsettings and environment variables
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("CICDMonitorDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:CICDMonitorDb is not configured. Set it via appsettings or environment variable 'ConnectionStrings__CICDMonitorDb'.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<CICDMonitorDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CICDMonitorDbContext(optionsBuilder.Options);
    }
}
