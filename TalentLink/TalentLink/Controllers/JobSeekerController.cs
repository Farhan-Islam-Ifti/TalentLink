using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentLink.Data;
using TalentLink.Models;
using TalentLink.Services;

namespace TalentLink.Controllers
{
    public class JobSeekerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICloudinaryService _cloudinaryService;

        public JobSeekerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _userManager = userManager;
            _cloudinaryService = cloudinaryService;
        }

        // GET: /JobSeeker/Profile/{userId}
        [HttpGet]
        public async Task<IActionResult> Profile(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var jsProfile = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == user.Id);
            if (jsProfile == null) return NotFound("Job seeker profile not found");

            var model = new JobSeekerEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = jsProfile.Address,
                DateOfBirth = jsProfile.DateOfBirth,
                Skills = jsProfile.Skills,
                Experience = jsProfile.Experience,
                Education = jsProfile.Education,
                Image = user.Image,
                CVFilePath = jsProfile.CVFilePath
            };

            return View("~/Views/JobSeeker/Profile.cshtml", model);
        }

        // GET: /JobSeeker/EditProfile/{userId}
        [HttpGet]
        public async Task<IActionResult> EditProfile(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var jsProfile = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == user.Id);
            if (jsProfile == null) return NotFound("Job seeker profile not found");

            var vm = new JobSeekerEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = jsProfile.Address,
                DateOfBirth = jsProfile.DateOfBirth,
                Skills = jsProfile.Skills,
                Experience = jsProfile.Experience,
                Education = jsProfile.Education,
                Image = user.Image,
                CVFilePath = jsProfile.CVFilePath
            };

            return View("~/Views/JobSeeker/EditProfile.cshtml", vm);
        }

        // POST: /JobSeeker/EditProfile
        [HttpPost]
        public async Task<IActionResult> EditProfile(JobSeekerEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/JobSeeker/EditProfile.cshtml", model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound("User not found");

            var jsProfile = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == user.Id);
            if (jsProfile == null) return NotFound("Job seeker profile not found");

            
            // Update only changed values (preserve old if new is null, empty, or whitespace)
            user.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? user.FirstName : model.FirstName;
            user.LastName = string.IsNullOrWhiteSpace(model.LastName) ? user.LastName : model.LastName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? user.PhoneNumber : model.PhoneNumber;

            jsProfile.Address = string.IsNullOrWhiteSpace(model.Address) ? jsProfile.Address : model.Address;
            jsProfile.Skills = string.IsNullOrWhiteSpace(model.Skills) ? jsProfile.Skills : model.Skills;
            jsProfile.Experience = string.IsNullOrWhiteSpace(model.Experience) ? jsProfile.Experience : model.Experience;
            jsProfile.Education = string.IsNullOrWhiteSpace(model.Education) ? jsProfile.Education : model.Education;

            // DateOfBirth is a nullable DateTime, so check differently
            jsProfile.DateOfBirth = model.DateOfBirth ?? jsProfile.DateOfBirth;


            // Upload Image if new file provided
            if (model.ImageFile != null)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(model.ImageFile);
                user.Image = imageUrl;
            }

            // Upload CV if new file provided
            if (model.CVFile != null)
            {
                var cvUrl = await _cloudinaryService.UploadPdfAsync(model.CVFile);
                jsProfile.CVFilePath = cvUrl;
            }

            await _userManager.UpdateAsync(user);
            _context.JobSeekers.Update(jsProfile);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile", new { userId = user.Id });
        }

        // GET: /JobSeeker/AvailableJobs
        [HttpGet]
        public async Task<IActionResult> AvailableJobs(string? search, string? location, JobType? jobType, int page = 1, int pageSize = 9)
        {
            var query = _context.JobPostings
                .Include(jp => jp.Company)
                .Where(jp => jp.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(jp => jp.Title.Contains(search) || jp.Description.Contains(search) || jp.Requirements.Contains(search));

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(jp => jp.Location.Contains(location));

            if (jobType.HasValue)
                query = query.Where(jp => jp.JobType == jobType);

            var totalCount = await query.CountAsync();
            var jobs = await query
                .OrderByDescending(jp => jp.PostedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new AvailableJobsViewModel
            {
                Jobs = jobs,
                Search = search,
                Location = location,
                JobType = jobType,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_JobsList", model);

            return View(model);
        }


    }
}
