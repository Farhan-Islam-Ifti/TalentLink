using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentLink.Data;
using TalentLink.Models;
using TalentLink.Services;

namespace TalentLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid registration data", errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                // Validate role-specific fields
                if (model.Role == UserRole.Company && string.IsNullOrEmpty(model.CompanyName))
                {
                    return BadRequest(new { message = "Company name is required for company registration" });
                }

                if (model.Role == UserRole.JobSeeker && (string.IsNullOrEmpty(model.FirstName) || string.IsNullOrEmpty(model.LastName)))
                {
                    return BadRequest(new { message = "First name and last name are required for job seeker registration" });
                }

                var result = await _authService.RegisterAsync(model);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);

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
                            Address = model.Address
                        };
                        _context.JobSeekers.Add(jobSeeker);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User registered successfully: {Email}", model.Email);
                    return Ok(new { message = "User registered successfully" });
                }

                return BadRequest(new { message = "User registration failed", errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid login data" });
                }

                var token = await _authService.LoginAsync(model);

                if (token == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Get user details to return with token
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                // Get role-specific profile
                object profile = null;
                if (user.Role == UserRole.Company)
                {
                    profile = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
                }
                else if (user.Role == UserRole.JobSeeker)
                {
                    profile = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == user.Id);
                }

                _logger.LogInformation("User logged in successfully: {Email}", model.Email);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.PhoneNumber,
                        user.Role,
                        profile
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login for {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        // Other methods (GetProfile, UpdateProfile, ChangePassword) remain the same as previous implementation
    }
}