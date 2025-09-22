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
    [Route("JobApplication")]
    public class JobApplicationMvcController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobApplicationMvcController> _logger;
        private readonly ICloudinaryService _cloudinaryService;

        public JobApplicationMvcController(
            ApplicationDbContext context,
            ILogger<JobApplicationMvcController> logger,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }

        // GET: JobApplication/MyApplications
        [Authorize(Roles = "JobSeeker")]
        [Route("MyApplications")]
        public async Task<IActionResult> MyApplications()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var jobSeeker = await _context.JobSeekers
                    .FirstOrDefaultAsync(js => js.UserId == userId);

                if (jobSeeker == null)
                {
                    return RedirectToAction("Create", "JobSeekerProfile");
                }

                var applications = await _context.JobApplications
                    .Include(ja => ja.JobPosting)
                    .ThenInclude(jp => jp.Company)
                    .Where(ja => ja.JobSeekerId == jobSeeker.Id)
                    .OrderByDescending(ja => ja.AppliedDate)
                    .ToListAsync();

                return View("~/Views/JobSeeker/MyApplications.cshtml", applications);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job applications");
                TempData["ErrorMessage"] = "Error retrieving your applications";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: JobApplication/CompanyApplications
        [Authorize(Roles = "Company")]
        [Route("CompanyApplications")]
        public async Task<IActionResult> CompanyApplications(int? jobPostingId = null, ApplicationStatus? status = null)
        {
            try
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
                    return RedirectToAction("Create", "CompanyProfile");
                }

                var query = _context.JobApplications
                    .Include(ja => ja.JobPosting)
                    .Include(ja => ja.JobSeeker)
                    .ThenInclude(js => js.User)
                    .Where(ja => ja.JobPosting.CompanyId == company.Id);

                if (jobPostingId.HasValue)
                {
                    query = query.Where(ja => ja.JobPostingId == jobPostingId.Value);
                    ViewBag.SelectedJobPostingId = jobPostingId.Value;
                }

                if (status.HasValue)
                {
                    query = query.Where(ja => ja.Status == status.Value);
                    ViewBag.SelectedStatus = status.Value;
                }

                var applications = await query
                    .OrderByDescending(ja => ja.AppliedDate)
                    .ToListAsync();

                // Get job postings for filter dropdown
                var jobPostings = await _context.JobPostings
                    .Where(jp => jp.CompanyId == company.Id)
                    .OrderByDescending(jp => jp.PostedDate)
                    .ToListAsync();

                ViewBag.JobPostings = jobPostings;
                ViewBag.Statuses = Enum.GetValues(typeof(ApplicationStatus));

                return View(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company applications");
                TempData["ErrorMessage"] = "Error retrieving applications";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: JobApplication/Details/{id}
        [Authorize]
        [Route("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                {
                    return Unauthorized();
                }

                var query = _context.JobApplications
                    .Include(ja => ja.JobPosting)
                        .ThenInclude(jp => jp.Company)
                    .Include(ja => ja.JobSeeker)
                        .ThenInclude(js => js.User)
                    .Include(ja => ja.Specialist)
                    .Where(ja => ja.Id == id);

                // Apply role-based filtering
                if (userRole == "JobSeeker")
                {
                    var jobSeeker = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == userId);
                    if (jobSeeker == null) return RedirectToAction("Create", "JobSeekerProfile");
                    query = query.Where(ja => ja.JobSeekerId == jobSeeker.Id);
                }
                else if (userRole == "Company")
                {
                    var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (company == null) return RedirectToAction("Create", "CompanyProfile");
                    query = query.Where(ja => ja.JobPosting.CompanyId == company.Id);
                }

                var application = await query.FirstOrDefaultAsync();

                if (application == null)
                {
                    return NotFound();
                }

                ViewBag.UserRole = userRole;
                return View("~/Views/JobSeeker/ApplicationDetails.cshtml", application);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application {ApplicationId}", id);
                TempData["ErrorMessage"] = "Error retrieving application details";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: JobApplication/Apply/{jobPostingId}
        [Authorize(Roles = "JobSeeker")]
        [Route("Apply/{jobPostingId}")]
        public async Task<IActionResult> Apply(int jobPostingId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var jobSeeker = await _context.JobSeekers
                    .FirstOrDefaultAsync(js => js.UserId == userId);

                if (jobSeeker == null)
                    return RedirectToAction("Create", "JobSeekerProfile");

                var jobPosting = await _context.JobPostings
                    .Include(jp => jp.Company)
                    .FirstOrDefaultAsync(jp => jp.Id == jobPostingId && jp.IsActive);

                if (jobPosting == null)
                    return NotFound();

                if (jobPosting.DeadlineDate < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "This job posting has expired.";
                    return RedirectToAction("Details", "JobPostingMvc", new { id = jobPostingId });
                }

                // Check if already applied
                var existingApplication = await _context.JobApplications
                    .FirstOrDefaultAsync(ja => ja.JobPostingId == jobPostingId && ja.JobSeekerId == jobSeeker.Id);

                if (existingApplication != null)
                {
                    TempData["InfoMessage"] = "You have already applied for this job.";
                    return RedirectToAction("Details", "JobPostingMvc", new { id = jobPostingId });
                }

                ViewBag.JobPosting = jobPosting;
                ViewBag.JobSeeker = jobSeeker;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading application form for job {JobPostingId}", jobPostingId);
                TempData["ErrorMessage"] = "Error loading application form";
                return RedirectToAction("Details", "JobPostingMvc", new { id = jobPostingId });
            }
        }

        // POST: JobApplication/Apply/{jobPostingId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobSeeker")]
        [Route("Apply/{jobPostingId}")]
        public async Task<IActionResult> Apply(int jobPostingId, ApplyJobViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var jobSeeker = await _context.JobSeekers
                    .FirstOrDefaultAsync(js => js.UserId == userId);

                if (jobSeeker == null)
                    return RedirectToAction("Create", "JobSeekerProfile");

                var jobPosting = await _context.JobPostings
                    .FirstOrDefaultAsync(jp => jp.Id == jobPostingId && jp.IsActive);

                if (jobPosting == null)
                    return NotFound();

                if (jobPosting.DeadlineDate < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "This job posting has expired.";
                    return RedirectToAction("Details", "JobPostingMvc", new { id = jobPostingId });
                }

                // Check if already applied
                var existingApplication = await _context.JobApplications
                    .FirstOrDefaultAsync(ja => ja.JobPostingId == jobPostingId && ja.JobSeekerId == jobSeeker.Id);

                if (existingApplication != null)
                {
                    TempData["InfoMessage"] = "You have already applied for this job.";
                    return RedirectToAction("Details", "JobPostingMvc", new { id = jobPostingId });
                }

                // Handle CV upload if provided
                string cvUrl = jobSeeker.CVFilePath;
                if (model.CVFile != null && model.CVFile.Length > 0)
                {
                    try
                    {
                        cvUrl = await _cloudinaryService.UploadPdfAsync(model.CVFile);
                        if (model.SaveCV)
                        {
                            jobSeeker.CVFilePath = cvUrl;
                            _context.JobSeekers.Update(jobSeeker);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading CV file");
                        ModelState.AddModelError("CVFile", "Error uploading CV file. Please try again.");
                        return View(model);
                    }
                }

                var jobApplication = new JobApplication
                {
                    JobPostingId = jobPostingId,
                    JobSeekerId = jobSeeker.Id,
                    CoverLetter = model.CoverLetter,
                    Status = ApplicationStatus.Submitted,
                    AppliedDate = DateTime.UtcNow
                };

                _context.JobApplications.Add(jobApplication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your application has been submitted successfully!";
                return RedirectToAction("Details", "JobPostingMvc", new { id = jobPostingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying for job {JobPostingId}", jobPostingId);
                TempData["ErrorMessage"] = "Error submitting application. Please try again.";
                return View(model);
            }
        }

        // GET: JobApplication/UpdateStatus/{id}
        [Authorize(Roles = "Company,Specialist")]
        [Route("UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                    return Unauthorized();

                var application = await _context.JobApplications
                    .Include(ja => ja.JobPosting)
                    .ThenInclude(jp => jp.Company)
                    .Include(ja => ja.JobSeeker)
                    .ThenInclude(js => js.User)
                    .FirstOrDefaultAsync(ja => ja.Id == id);

                if (application == null)
                    return NotFound();

                // Check permissions
                if (userRole == "Company")
                {
                    var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (company == null || application.JobPosting.CompanyId != company.Id)
                        return Forbid();
                }

                ViewBag.Application = application;
                ViewBag.UserRole = userRole;
                ViewBag.Statuses = Enum.GetValues(typeof(ApplicationStatus));

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading update status form for application {ApplicationId}", id);
                TempData["ErrorMessage"] = "Error loading update form";
                return RedirectToAction("Details", new { id });
            }
        }

        // POST: JobApplication/UpdateStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company,Specialist")]
        [Route("UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateApplicationStatusViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                    return Unauthorized();

                var application = await _context.JobApplications
                    .Include(ja => ja.JobPosting)
                    .ThenInclude(jp => jp.Company)
                    .FirstOrDefaultAsync(ja => ja.Id == id);

                if (application == null)
                    return NotFound();

                // Check permissions
                if (userRole == "Company")
                {
                    var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (company == null || application.JobPosting.CompanyId != company.Id)
                        return Forbid();
                }

                // Update application
                application.Status = model.Status;
                application.InterviewDate = model.InterviewDate;
                application.InterviewNotes = model.InterviewNotes;

                // Assign specialist if updating as specialist
                if (userRole == "Specialist" && application.SpecialistId == null)
                    application.SpecialistId = userId;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Application status updated successfully!";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application status for application {ApplicationId}", id);
                TempData["ErrorMessage"] = "Error updating application status";
                return RedirectToAction("Details", new { id });
            }
        }
    }

    public class ApplyJobViewModel
    {
        [Required(ErrorMessage = "Cover letter is required")]
        [StringLength(2000, ErrorMessage = "Cover letter cannot exceed 2000 characters")]
        public string CoverLetter { get; set; } = string.Empty;

        [Display(Name = "Upload CV (optional)")]
        public IFormFile? CVFile { get; set; }

        [Display(Name = "Save this CV as my default")]
        public bool SaveCV { get; set; } = true;
    }

    public class UpdateApplicationStatusViewModel
    {
        [Required(ErrorMessage = "Status is required")]
        public ApplicationStatus Status { get; set; }

        [Display(Name = "Interview Date")]
        public DateTime? InterviewDate { get; set; }

        [Display(Name = "Interview Notes")]
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? InterviewNotes { get; set; }
    }
}