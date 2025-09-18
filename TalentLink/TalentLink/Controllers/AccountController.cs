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
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            IAuthService authService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _authService = authService;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Redirect to Login page since registration is now embedded there
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            var loginModel = new LoginModel();
            return View(loginModel);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                // Store registration data and errors in TempData to pass to Login view
                TempData["ShowRegisterForm"] = true;
                TempData["RegisterModel"] = System.Text.Json.JsonSerializer.Serialize(model);

                // Store ModelState errors
                var errors = new Dictionary<string, List<string>>();
                foreach (var key in ModelState.Keys)
                {
                    var modelErrors = ModelState[key]?.Errors;
                    if (modelErrors != null && modelErrors.Count > 0)
                    {
                        errors[key] = modelErrors.Select(e => e.ErrorMessage).ToList();
                    }
                }
                TempData["RegisterErrors"] = System.Text.Json.JsonSerializer.Serialize(errors);

                return RedirectToAction("Login");
            }

            var result = await _authService.RegisterAsync(model);

            if (result.Succeeded)
            {
                // Get the newly created user
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // Create role-specific profile
                    if (model.Role == UserRole.Company)
                    {
                        var company = new Company
                        {
                            UserId = user.Id,
                            CompanyName = model.CompanyName ?? string.Empty,
                            Industry = model.Industry ?? string.Empty,
                            Website = model.Website ?? string.Empty,
                            Address = model.CompanyAddress ?? string.Empty,
                            Description = model.CompanyDescription ?? string.Empty
                        };
                        _context.Companies.Add(company);
                    }
                    else if (model.Role == UserRole.JobSeeker)
                    {
                        var jobSeeker = new JobSeeker
                        {
                            UserId = user.Id,
                            Skills = model.Skills ?? string.Empty,
                            Experience = model.Experience ?? string.Empty,
                            Education = model.Education ?? string.Empty,
                            DateOfBirth = model.DateOfBirth ?? DateTime.Now.AddYears(-20),
                            Address = model.Address ?? string.Empty,
                            CVFilePath = null // Handle file upload separately via API
                        };
                        _context.JobSeekers.Add(jobSeeker);
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            // If registration failed, show errors on the register form
            TempData["ShowRegisterForm"] = true;
            TempData["RegisterModel"] = System.Text.Json.JsonSerializer.Serialize(model);

            var registerErrors = new Dictionary<string, List<string>>();
            foreach (var error in result.Errors)
            {
                if (!registerErrors.ContainsKey("RegisterGeneral"))
                    registerErrors["RegisterGeneral"] = new List<string>();
                registerErrors["RegisterGeneral"].Add(error.Description);
            }
            TempData["RegisterErrors"] = System.Text.Json.JsonSerializer.Serialize(registerErrors);

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // Use SignInManager for cookie authentication
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // Store in session
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("FirstName", user.FirstName);
            HttpContext.Session.SetString("Role", user.Role.ToString());
            HttpContext.Session.SetString("Email", user.Email);

            // Store in TempData for JavaScript access
            TempData["UserId"] = user.Id;
            TempData["FirstName"] = user.FirstName;

            // Redirect to dashboard
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Use SignInManager to sign out
            _signInManager.SignOutAsync();

            // Remove AuthToken cookie
            Response.Cookies.Delete("AuthToken");

            // Clear session
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("FirstName");
            HttpContext.Session.Remove("Role");
            HttpContext.Session.Remove("Email");

            return RedirectToAction("Index", "Home");
        }
    }

    // View Models for Profile Views (keep these as they are)
    public class JobSeekerProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public string Skills { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string? CVFilePath { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
    }

    public class CompanyProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AdminProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
    }
}