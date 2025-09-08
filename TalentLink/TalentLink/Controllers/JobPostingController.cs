using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TalentLink.Data;
using TalentLink.Models;
using TalentLink.Services;

namespace TalentLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobPostingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobPostingController> _logger;
        private readonly ICloudinaryService _cloudinaryService;

        public JobPostingController(
            ApplicationDbContext context,
            ILogger<JobPostingController> logger,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }

        // GET: api/JobPosting
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllJobPostings(
            [FromQuery] string? search = null,
            [FromQuery] string? location = null,
            [FromQuery] JobType? jobType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.JobPostings
                    .Include(jp => jp.Company)
                    .Where(jp => jp.IsActive);

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(jp => jp.Title.Contains(search) ||
                                            jp.Description.Contains(search) ||
                                            jp.Requirements.Contains(search));
                }

                if (!string.IsNullOrEmpty(location))
                {
                    query = query.Where(jp => jp.Location.Contains(location));
                }

                if (jobType.HasValue)
                {
                    query = query.Where(jp => jp.JobType == jobType);
                }

                var totalCount = await query.CountAsync();
                var jobPostings = await query
                    .OrderByDescending(jp => jp.PostedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(jp => new
                    {
                        jp.Id,
                        jp.Title,
                        jp.Description,
                        jp.Requirements,
                        jp.Salary,
                        jp.Location,
                        jp.JobType,
                        jp.PostedDate,
                        jp.DeadlineDate,
                        Company = new
                        {
                            jp.Company.CompanyName,
                            jp.Company.Industry,
                            jp.Company.Website
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = jobPostings,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job postings");
                return StatusCode(500, new { message = "Error retrieving job postings" });
            }
        }

        // GET: api/JobPosting/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetJobPosting(int id)
        {
            try
            {
                var jobPosting = await _context.JobPostings
                    .Include(jp => jp.Company)
                    .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(jp => jp.Id == id && jp.IsActive);

                if (jobPosting == null)
                {
                    return NotFound(new { message = "Job posting not found" });
                }

                return Ok(new
                {
                    jobPosting.Id,
                    jobPosting.Title,
                    jobPosting.Description,
                    jobPosting.Requirements,
                    jobPosting.Salary,
                    jobPosting.Location,
                    jobPosting.JobType,
                    jobPosting.PostedDate,
                    jobPosting.DeadlineDate,
                    Company = new
                    {
                        jobPosting.Company.CompanyName,
                        jobPosting.Company.Industry,
                        jobPosting.Company.Website,
                        jobPosting.Company.Address,
                        jobPosting.Company.Description
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job posting {JobPostingId}", id);
                return StatusCode(500, new { message = "Error retrieving job posting" });
            }
        }

        // POST: api/JobPosting
        [HttpPost]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> CreateJobPosting([FromBody] CreateJobPostingModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
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
                    return BadRequest(new { message = "Company profile not found" });
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

                _logger.LogInformation("Job posting created successfully by company {CompanyId}", company.Id);

                return CreatedAtAction(nameof(GetJobPosting), new { id = jobPosting.Id }, new
                {
                    jobPosting.Id,
                    jobPosting.Title,
                    jobPosting.Description,
                    jobPosting.Requirements,
                    jobPosting.Salary,
                    jobPosting.Location,
                    jobPosting.JobType,
                    jobPosting.PostedDate,
                    jobPosting.DeadlineDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job posting");
                return StatusCode(500, new { message = "Error creating job posting" });
            }
        }

        // PUT: api/JobPosting/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> UpdateJobPosting(int id, [FromBody] UpdateJobPostingModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
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
                    return BadRequest(new { message = "Company profile not found" });
                }

                var jobPosting = await _context.JobPostings
                    .FirstOrDefaultAsync(jp => jp.Id == id && jp.CompanyId == company.Id);

                if (jobPosting == null)
                {
                    return NotFound(new { message = "Job posting not found or you don't have permission to update it" });
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

                _logger.LogInformation("Job posting {JobPostingId} updated successfully", id);

                return Ok(new { message = "Job posting updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job posting {JobPostingId}", id);
                return StatusCode(500, new { message = "Error updating job posting" });
            }
        }

        // DELETE: api/JobPosting/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> DeleteJobPosting(int id)
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
                    return BadRequest(new { message = "Company profile not found" });
                }

                var jobPosting = await _context.JobPostings
                    .FirstOrDefaultAsync(jp => jp.Id == id && jp.CompanyId == company.Id);

                if (jobPosting == null)
                {
                    return NotFound(new { message = "Job posting not found or you don't have permission to delete it" });
                }

                // Soft delete by setting IsActive to false
                jobPosting.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Job posting {JobPostingId} deleted successfully", id);

                return Ok(new { message = "Job posting deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job posting {JobPostingId}", id);
                return StatusCode(500, new { message = "Error deleting job posting" });
            }
        }

        // GET: api/JobPosting/company/my-postings
        [HttpGet("company/my-postings")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> GetMyJobPostings()
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
                    return BadRequest(new { message = "Company profile not found" });
                }

                var jobPostings = await _context.JobPostings
                    .Where(jp => jp.CompanyId == company.Id)
                    .OrderByDescending(jp => jp.PostedDate)
                    .Select(jp => new
                    {
                        jp.Id,
                        jp.Title,
                        jp.Description,
                        jp.Requirements,
                        jp.Salary,
                        jp.Location,
                        jp.JobType,
                        jp.PostedDate,
                        jp.DeadlineDate,
                        jp.IsActive,
                        ApplicationCount = jp.JobApplications.Count()
                    })
                    .ToListAsync();

                return Ok(jobPostings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company job postings");
                return StatusCode(500, new { message = "Error retrieving job postings" });
            }
        }

        // POST: api/JobPosting/{id}/apply
        [HttpPost("{id}/apply")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> ApplyForJob(int id, [FromForm] ApplyJobModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var jobSeeker = await _context.JobSeekers
                    .FirstOrDefaultAsync(js => js.UserId == userId);

                if (jobSeeker == null)
                {
                    return BadRequest(new { message = "Job seeker profile not found" });
                }

                var jobPosting = await _context.JobPostings
                    .FirstOrDefaultAsync(jp => jp.Id == id && jp.IsActive);

                if (jobPosting == null)
                {
                    return NotFound(new { message = "Job posting not found or inactive" });
                }

                // Check if already applied
                var existingApplication = await _context.JobApplications
                    .FirstOrDefaultAsync(ja => ja.JobPostingId == id && ja.JobSeekerId == jobSeeker.Id);

                if (existingApplication != null)
                {
                    return BadRequest(new { message = "You have already applied for this job" });
                }

                // Handle CV upload if provided
                string? cvUrl = null;
                if (model.CVFile != null)
                {
                    cvUrl = await _cloudinaryService.UploadPdfAsync(model.CVFile);
                    // Update job seeker's CV path
                    jobSeeker.CVFilePath = cvUrl;
                }

                var jobApplication = new JobApplication
                {
                    JobPostingId = id,
                    JobSeekerId = jobSeeker.Id,
                    CoverLetter = model.CoverLetter,
                    Status = ApplicationStatus.Submitted,
                    AppliedDate = DateTime.UtcNow
                };

                _context.JobApplications.Add(jobApplication);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Job application submitted successfully for JobPosting {JobPostingId} by JobSeeker {JobSeekerId}", id, jobSeeker.Id);

                return Ok(new { message = "Application submitted successfully", applicationId = jobApplication.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying for job {JobPostingId}", id);
                return StatusCode(500, new { message = "Error submitting application" });
            }
        }
    }

    // Model classes
    public class CreateJobPostingModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public DateTime? DeadlineDate { get; set; }
    }

    public class UpdateJobPostingModel : CreateJobPostingModel
    {
        public bool IsActive { get; set; } = true;
    }

    public class ApplyJobModel
    {
        public string CoverLetter { get; set; } = string.Empty;
        public IFormFile? CVFile { get; set; }
    }
}