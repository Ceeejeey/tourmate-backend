using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TourMate.API.Models;

namespace TourMate.API.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        try
        {
            // Ensure the database is created and migrations applied
            await context.Database.MigrateAsync();

            if (!await context.Users.AnyAsync(u => u.Role == "admin"))
            {
                logger.LogInformation("Seeding default admin user...");
                
                var adminUser = new User
                {
                    Name = "Admin",
                    Email = "admin@tourmate.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Default password, change in production
                    Role = "admin"
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Default admin user created successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
