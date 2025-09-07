using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TalentLink.Models;

namespace TalentLink.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Check if database is ready
                try
                {
                    // Simple query to check if database is accessible
                    var canConnect = await context.Database.CanConnectAsync();
                    if (!canConnect)
                    {
                        Console.WriteLine("Database is not accessible yet.");
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("Database is not ready for seeding.");
                    return;
                }

                // Create roles
                string[] roleNames = { "Admin", "Company", "JobSeeker", "Specialist" };

                foreach (var roleName in roleNames)
                {
                    // Check if role exists using a safe approach
                    var roleExists = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExists)
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                        Console.WriteLine($"Created role: {roleName}");
                    }
                }

                // Create admin user
                var adminUser = await userManager.FindByEmailAsync("admin@talentlink.com");
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin@talentlink.com",
                        Email = "admin@talentlink.com",
                        FirstName = "Admin",
                        LastName = "User",
                        Role = UserRole.Admin,
                        EmailConfirmed = true
                    };

                    var createAdmin = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (createAdmin.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine("Created admin user: admin@talentlink.com");
                    }
                    else
                    {
                        Console.WriteLine("Failed to create admin user: " + string.Join(", ", createAdmin.Errors.Select(e => e.Description)));
                    }
                }

                // Save changes to ensure users are persisted before creating related entities
                await context.SaveChangesAsync();

                Console.WriteLine("Database seeding completed successfully.");
            }
        }
    }
}