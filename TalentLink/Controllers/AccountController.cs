using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentLink.Data;
using TalentLink.Models;
using TalentLink.Services;

namespace TalentLink.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public AccountController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);

            if (result.Succeeded)
            {
                var user = await _authService.GetUserByIdAsync((await _authService.GetUserByIdAsync(model.Email))?.Id);

                if (user != null)
                {
                    // Role-specific profile
                    if (model.Role == UserRole.Company)
                    {
                        var company = new Company
                        {
                            UserId = user.Id,
                            CompanyName = model.CompanyName,
                            Industry = model.Industry,
                            Website = model.Website,
                            Address = model.CompanyAddress,
                            Description = model.CompanyDescription
                        };
                        _context.Companies.Add(company);
                    }
                    else if (model.Role == UserRole.JobSeeker)
                    {
                        var jobSeeker = new JobSeeker
                        {
                            UserId = user.Id,
                            Skills = model.Skills,
                            Experience = model.Experience,
                            Education = model.Education,
                            DateOfBirth = model.DateOfBirth ?? DateTime.Now.AddYears(-20),
                            Address = model.Address,
                            CVFilePath = "" // TODO: Handle file upload
                        };
                        _context.JobSeekers.Add(jobSeeker);
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = await _authService.LoginAsync(model);

            if (token == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Save token in cookie
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(7)
            });

            // Get logged-in user
            var user = await _authService.GetUserByIdAsync((await _authService.GetUserByIdAsync(model.Email))?.Id);

            if (user != null)
            {
                switch (user.Role)
                {
                    case UserRole.Admin:
                        return RedirectToAction("Dashboard", "Admin");
                    case UserRole.Company:
                        return RedirectToAction("Dashboard", "Company");
                    case UserRole.JobSeeker:
                        return RedirectToAction("Dashboard", "JobSeeker");
                    default:
                        return RedirectToAction("Dashboard", "Home");
                }
            }

            return RedirectToAction("Dashboard", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Index", "Home");
        }
    }
}
