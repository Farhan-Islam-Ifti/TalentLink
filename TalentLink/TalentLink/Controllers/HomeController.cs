using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentLink.Data;
using TalentLink.Models;
using System.Diagnostics;

namespace TalentLink.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                // Get active job postings with company information
                var jobPostings = await _context.JobPostings
                    .Include(j => j.Company)
                    .Where(j => j.IsActive && j.DeadlineDate > DateTime.UtcNow)
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                ViewBag.JobPostings = jobPostings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading job postings");
                ViewBag.JobPostings = new List<JobPosting>();
            }

            ViewBag.UserId = userId;
            ViewBag.UserRole = userRole;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult About()
        {
            return View(); // Views/Home/About.cshtml will be used
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}