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

            // Update only changed values
            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

            jsProfile.Address = model.Address ?? jsProfile.Address;
            jsProfile.DateOfBirth = model.DateOfBirth ?? jsProfile.DateOfBirth;
            jsProfile.Skills = model.Skills ?? jsProfile.Skills;
            jsProfile.Experience = model.Experience ?? jsProfile.Experience;
            jsProfile.Education = model.Education ?? jsProfile.Education;

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
    }
}
