using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TalentLink.Data;
using TalentLink.Models;
using TalentLink.Services;

namespace TalentLink.Controllers
{
    [Authorize]
    public class JobPostingMvcController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobPostingMvcController> _logger;
        private readonly ICloudinaryService _cloudinaryService;

        public JobPostingMvcController(
            ApplicationDbContext context,
            ILogger<JobPostingMvcController> logger,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }

        // GET: JobPosting/Create
        [Authorize(Roles = "Company")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: JobPosting/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Create(CreateJobPostingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                ModelState.AddModelError("", "Company profile not found. Please complete your company profile first.");
                return View(model);
            }

            var jobPosting = new JobPosting
            {
                CompanyId = company.Id,
                Title = model.Title,
                Description = model.Description,
                Requirements = model.Requirements,
                Salary = model.Salary,
                Location = model.Location,
                JobType = model.JobType,
                DeadlineDate = model.DeadlineDate,
                PostedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job posted successfully!";
            return RedirectToAction("MyPostings");
        }

        // GET: JobPosting/MyPostings
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> MyPostings()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                TempData["ErrorMessage"] = "Company profile not found. Please complete your company profile first.";
                return RedirectToAction("Create", "CompanyProfile");
            }

            var jobPostings = await _context.JobPostings
                .Where(jp => jp.CompanyId == company.Id)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

            return View(jobPostings);
        }

        // GET: JobPosting/Edit/{id}
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return RedirectToAction("Create", "CompanyProfile");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.Id == id && jp.CompanyId == company.Id);

            if (jobPosting == null)
            {
                return NotFound();
            }

            var model = new EditJobPostingViewModel
            {
                Id = jobPosting.Id,
                Title = jobPosting.Title,
                Description = jobPosting.Description,
                Requirements = jobPosting.Requirements,
                Salary = jobPosting.Salary,
                Location = jobPosting.Location,
                JobType = jobPosting.JobType,
                DeadlineDate = jobPosting.DeadlineDate,
                IsActive = jobPosting.IsActive
            };

            return View(model);
        }

        // POST: JobPosting/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Edit(int id, EditJobPostingViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return RedirectToAction("Create", "CompanyProfile");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.Id == id && jp.CompanyId == company.Id);

            if (jobPosting == null)
            {
                return NotFound();
            }

            jobPosting.Title = model.Title;
            jobPosting.Description = model.Description;
            jobPosting.Requirements = model.Requirements;
            jobPosting.Salary = model.Salary;
            jobPosting.Location = model.Location;
            jobPosting.JobType = model.JobType;
            jobPosting.DeadlineDate = model.DeadlineDate;
            jobPosting.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job posting updated successfully!";
            return RedirectToAction("MyPostings");
        }

        // GET: JobPosting/Details/{id}
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var jobPosting = await _context.JobPostings
                    .Include(jp => jp.Company)
                    .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(jp => jp.Id == id && jp.IsActive);

                if (jobPosting == null)
                {
                    return NotFound();
                }

                // Check if user is the company owner (for edit/delete options)
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCompanyOwner = false;

                if (!string.IsNullOrEmpty(userId))
                {
                    var company = await _context.Companies
                        .FirstOrDefaultAsync(c => c.UserId == userId);

                    isCompanyOwner = company != null && company.Id == jobPosting.CompanyId;
                }

                ViewBag.IsCompanyOwner = isCompanyOwner;
                return View(jobPosting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job posting {JobPostingId}", id);
                return StatusCode(500, new { message = "Error retrieving job posting" });
            }
        }

        // POST: JobPosting/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return RedirectToAction("Create", "CompanyProfile");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.Id == id && jp.CompanyId == company.Id);

            if (jobPosting == null)
            {
                return NotFound();
            }

            _context.JobPostings.Remove(jobPosting);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job posting deleted successfully!";
            return RedirectToAction("MyPostings");
        }
    }

    public class CreateJobPostingViewModel
    {
        [Required(ErrorMessage = "Job title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Requirements are required")]
        public string Requirements { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive value")]
        public decimal? Salary { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job type is required")]
        public JobType JobType { get; set; }

        [Required(ErrorMessage = "Deadline date is required")]
        [FutureDate(ErrorMessage = "Deadline must be a future date")]
        public DateTime? DeadlineDate { get; set; }
    }

    public class EditJobPostingViewModel : CreateJobPostingViewModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateTime date)
            {
                return date > DateTime.Now;
            }
            return false;
        }
    }
}