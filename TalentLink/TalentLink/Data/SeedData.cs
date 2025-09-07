using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using TalentLink.Data;
using TalentLink.Models;

namespace TalentLink.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Seed roles
            string[] roles = { "Admin", "Company", "JobSeeker" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed sample user and company
            var companyUser = new ApplicationUser
            {
                UserName = "company@example.com",
                Email = "company@example.com",
                FirstName = "Company",
                LastName = "Admin",
                Role = UserRole.Company
            };

            if (await userManager.FindByEmailAsync(companyUser.Email) == null)
            {
                await userManager.CreateAsync(companyUser, "Password123!");
                await userManager.AddToRoleAsync(companyUser, "Company");
            }

            var company = new Company
            {
                UserId = companyUser.Id,
                CompanyName = "Sample Company",
                Industry = "Tech",
                Address = "123 Tech Street",
                Website = "https://sample.com",
                Description = "A sample company",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (!context.Companies.Any(c => c.CompanyName == company.CompanyName))
            {
                context.Companies.Add(company);
                await context.SaveChangesAsync();
            }

            // Seed sample job posting
            var jobPosting = new JobPosting
            {
                CompanyId = company.Id,
                Title = "Software Engineer",
                Description = "Develop awesome software",
                Requirements = "C#, .NET",
                Salary = 100000.00m,
                Location = "Remote",
                JobType = JobType.FullTime,
                IsActive = true,
                PostedDate = DateTime.UtcNow,
                DeadlineDate = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (!context.JobPostings.Any(j => j.Title == jobPosting.Title))
            {
                context.JobPostings.Add(jobPosting);
                await context.SaveChangesAsync();
            }
        }
    }
}