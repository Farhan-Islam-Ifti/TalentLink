using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentLink.Data;
using TalentLink.Models;
using TalentLink.Services;

namespace TalentLink.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICloudinaryService _cloudinaryService;

        public CompanyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _userManager = userManager;
            _cloudinaryService = cloudinaryService;
        }

        // GET: /Company/Profile/{userId}
        [HttpGet]
        public async Task<IActionResult> Profile(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (company == null) return NotFound("Company profile not found");

            var model = new CompanyEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                CompanyName = company.CompanyName,
                Industry = company.Industry,
                Website = company.Website,
                Address = company.Address,
                Description = company.Description,
                Image = user.Image
            };

            return View("~/Views/Company/Profile.cshtml", model);
        }

        // GET: /Company/EditProfile/{userId}
        [HttpGet]
        public async Task<IActionResult> EditProfile(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (company == null) return NotFound("Company profile not found");

            var vm = new CompanyEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                CompanyName = company.CompanyName,
                Industry = company.Industry,
                Website = company.Website,
                Address = company.Address,
                Description = company.Description,
                Image = user.Image
            };

            return View("~/Views/Company/EditProfile.cshtml", vm);
        }

        // POST: /Company/EditProfile
        [HttpPost]
        public async Task<IActionResult> EditProfile(CompanyEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Company/EditProfile.cshtml", model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound("User not found");

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (company == null) return NotFound("Company profile not found");

            // Update only changed values
            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

            company.CompanyName = model.CompanyName ?? company.CompanyName;
            company.Industry = model.Industry ?? company.Industry;
            company.Website = model.Website ?? company.Website;
            company.Address = model.Address ?? company.Address;
            company.Description = model.Description ?? company.Description;

            // Upload Image if new file provided
            if (model.ImageFile != null)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(model.ImageFile);
                user.Image = imageUrl;
            }

            await _userManager.UpdateAsync(user);
            _context.Companies.Update(company);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile", new { userId = user.Id });
        }
    }
}

