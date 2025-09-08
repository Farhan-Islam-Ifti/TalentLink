using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(IAuthService authService, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _context = context;
            _userManager = userManager;
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
                // Get the newly created user
                var user = await _authService.GetUserByIdAsync((await _authService.GetUserByIdAsync(model.Email))?.Id);

                if (user != null)
                {
                    // Create role-specific profile
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
                            CVFilePath = "" // You'll need to handle file upload separately
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

            // Store token in a cookie for authentication
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(7)
            });

            // Get user to determine role-based redirect
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }
            // Store in session (backend)
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("FirstName", user.FirstName);

            // Pass info via query string to dashboard or home page
            return RedirectToAction("Index", "Home", new { userId = user.Id, firstName = user.FirstName });
        }


        [HttpGet]
        public IActionResult Logout()
        {
            // Remove AuthToken cookie
            Response.Cookies.Delete("AuthToken");

            // Clear session
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("FirstName");

            return RedirectToAction("Index", "Home");
        }
    }
}