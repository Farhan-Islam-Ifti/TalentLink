using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentLink.Data;
using TalentLink.Models;
using System.Security.Claims;

namespace TalentLink.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Get current user ID from claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get user role from claims
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Company")
            {
                return RedirectToAction("CompanyDashboard");
            }
            else if (userRole == "JobSeeker")
            {
                return RedirectToAction("JobSeekerDashboard");
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Company")]
        public async Task<IActionResult> CompanyDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var company = await _context.Companies
                .Include(c => c.JobPostings)
                    .ThenInclude(j => j.JobApplications)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return RedirectToAction("Profile", "Account", new { userId });
            }

            // Statistics
            var totalJobs = company.JobPostings.Count;

            var activeJobs = company.JobPostings
                .Where(j => j.IsActive && j.DeadlineDate.HasValue && j.DeadlineDate > DateTime.UtcNow)
                .Count();

            var totalApplications = company.JobPostings.Sum(j => j.JobApplications.Count);

            // Get recent applications
            var recentApplications = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Where(a => a.JobPosting.Company.UserId == userId)
                .OrderByDescending(a => a.AppliedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalJobs = totalJobs;
            ViewBag.ActiveJobs = activeJobs;
            ViewBag.TotalApplications = totalApplications;
            ViewBag.RecentApplications = recentApplications;

            return View(company);
        }

        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> JobSeekerDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.User)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return RedirectToAction("Profile", "Account", new { userId });
            }
            // Get recommended jobs
            var recommendedJobs = await _context.JobPostings
                .Include(j => j.Company)
                .Where(j => j.IsActive && j.DeadlineDate.HasValue && j.DeadlineDate > DateTime.UtcNow)
                .OrderByDescending(j => j.CreatedAt)
                .Take(6)
                .ToListAsync();

            // Get user's applications
            var applications = await _context.JobApplications
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.Company)
                .Where(a => a.JobSeeker.UserId == userId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            ViewBag.RecommendedJobs = recommendedJobs;
            ViewBag.Applications = applications;

            return View(jobSeeker);
        }
    }
}