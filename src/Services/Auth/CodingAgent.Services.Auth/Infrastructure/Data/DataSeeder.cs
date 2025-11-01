using CodingAgent.Services.Auth.Domain.Entities;
using CodingAgent.Services.Auth.Infrastructure.Persistence;
using CodingAgent.Services.Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services.Auth.Infrastructure.Data;

/// <summary>
/// Seeds default admin user in the database
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Seeds the default admin user if it doesn't exist
    /// </summary>
    public static async Task SeedAdminUserAsync(
        IServiceProvider serviceProvider,
        string username = "admin",
        string password = "Admin123!",
        string email = "admin@example.com")
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DataSeeder");

        // Check if admin user already exists
        var adminExists = await dbContext.Users
            .AnyAsync(u => u.Username == username || u.Email == email);

        if (adminExists)
        {
            logger.LogInformation("Admin user '{Username}' already exists, skipping seed", username);
            
            // Update roles to ensure admin role is set
            var admin = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
            
            if (admin != null && !admin.Roles.Contains("Admin"))
            {
                admin.UpdateRoles("Admin,User");
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Updated admin user roles to include Admin");
            }
            return;
        }

        // Create admin user
        var passwordHash = passwordHasher.HashPassword(password);
        var adminUser = new User(username, email, passwordHash, "Admin,User");

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Successfully seeded admin user '{Username}'", username);
    }
}


