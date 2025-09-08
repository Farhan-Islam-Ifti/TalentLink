using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentLink.Data;
using TalentLink.Models;
using TalentLink.Services;
using System.Security.Claims;

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
        private readonly ICloudinaryService _cloudinaryService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<AuthController> logger,
            ICloudinaryService cloudinaryService,
            SignInManager<ApplicationUser> signInManager)
        {
            _authService = authService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
            _signInManager = signInManager;
        }


        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromForm] RegisterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid registration data", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
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

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists" });
                }

                var result = await _authService.RegisterAsync(model);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        throw new Exception("User creation succeeded but user not found");
                    }

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
                        _logger.LogInformation("Creating company profile for user {UserId}", user.Id);
                    }
                    else if (model.Role == UserRole.JobSeeker)
                    {
                        // Handle CV upload if provided
                        string? cvUrl = null;
                        if (model.CVFile != null)
                        {
                            cvUrl = await _cloudinaryService.UploadPdfAsync(model.CVFile);
                        }

                        var jobSeeker = new JobSeeker
                        {
                            UserId = user.Id,
                            Skills = model.Skills ?? string.Empty,
                            Experience = model.Experience ?? string.Empty,
                            Education = model.Education ?? string.Empty,
                            DateOfBirth = model.DateOfBirth ?? DateTime.Now.AddYears(-20),
                            Address = model.Address ?? string.Empty,
                            CVFilePath = cvUrl
                        };

                        _context.JobSeekers.Add(jobSeeker);
                        _logger.LogInformation("Creating job seeker profile for user {UserId}", user.Id);
                    }

                    // Save changes
                    var saveResult = await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved {Count} entities to database", saveResult);

                    await transaction.CommitAsync();

                    _logger.LogInformation("User registered successfully: {Email} with role {Role}", model.Email, model.Role);
                    return Ok(new { message = "User registered successfully" });
                }

                await transaction.RollbackAsync();
                return BadRequest(new { message = "User registration failed", errors = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during user registration for {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred during registration", error = ex.Message });
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

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Use SignInManager for cookie authentication
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Store in session
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("FirstName", user.FirstName);
                HttpContext.Session.SetString("Role", user.Role.ToString());
                HttpContext.Session.SetString("Email", user.Email);

                // Get role-specific profile
                object? profile = null;
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
                    message = "Login successful",
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

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                object? profile = null;
                if (user.Role == UserRole.Company)
                {
                    profile = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                }
                else if (user.Role == UserRole.JobSeeker)
                {
                    profile = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == userId);
                }

                return Ok(new
                {
                    user = new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.PhoneNumber,
                        user.Role
                    },
                    profile
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new { message = "Error retrieving profile" });
            }
        }

        [HttpPost("upload-cv")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> UploadCV([FromForm] IFormFile cvFile)
        {
            try
            {
                if (cvFile == null || cvFile.Length == 0)
                {
                    return BadRequest(new { message = "No file provided" });
                }

                // Validate file type (PDF only)
                if (!cvFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Only PDF files are allowed" });
                }

                // Validate file size (max 5MB)
                if (cvFile.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File size cannot exceed 5MB" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var jobSeeker = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == userId);
                if (jobSeeker == null)
                {
                    return BadRequest(new { message = "Job seeker profile not found" });
                }

                var cvUrl = await _cloudinaryService.UploadPdfAsync(cvFile);

                jobSeeker.CVFilePath = cvUrl;
                await _context.SaveChangesAsync();

                return Ok(new { message = "CV uploaded successfully", cvUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading CV");
                return StatusCode(500, new { message = "Error uploading CV" });
            }
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileModel model)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update basic user info only if provided
                if (!string.IsNullOrEmpty(model.FirstName))
                    user.FirstName = model.FirstName;

                if (!string.IsNullOrEmpty(model.LastName))
                    user.LastName = model.LastName;

                if (!string.IsNullOrEmpty(model.PhoneNumber))
                    user.PhoneNumber = model.PhoneNumber;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update user", errors = updateResult.Errors.Select(e => e.Description) });
                }

                // Update role-specific profile
                if (user.Role == UserRole.Company)
                {
                    var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (company != null)
                    {
                        if (!string.IsNullOrEmpty(model.CompanyName))
                            company.CompanyName = model.CompanyName;

                        if (!string.IsNullOrEmpty(model.Industry))
                            company.Industry = model.Industry;

                        if (!string.IsNullOrEmpty(model.Website))
                            company.Website = model.Website;

                        if (!string.IsNullOrEmpty(model.Address))
                            company.Address = model.Address;

                        if (!string.IsNullOrEmpty(model.Description))
                            company.Description = model.Description;
                    }
                }
                else if (user.Role == UserRole.JobSeeker)
                {
                    var jobSeeker = await _context.JobSeekers.FirstOrDefaultAsync(js => js.UserId == userId);
                    if (jobSeeker != null)
                    {
                        if (!string.IsNullOrEmpty(model.Skills))
                            jobSeeker.Skills = model.Skills;

                        if (!string.IsNullOrEmpty(model.Experience))
                            jobSeeker.Experience = model.Experience;

                        if (!string.IsNullOrEmpty(model.Education))
                            jobSeeker.Education = model.Education;

                        if (!string.IsNullOrEmpty(model.Address))
                            jobSeeker.Address = model.Address;

                        if (model.DateOfBirth.HasValue)
                            jobSeeker.DateOfBirth = model.DateOfBirth.Value;

                        // Handle CV upload if provided
                        if (model.CVFile != null)
                        {
                            var cvUrl = await _cloudinaryService.UploadPdfAsync(model.CVFile);
                            jobSeeker.CVFilePath = cvUrl;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return StatusCode(500, new { message = "Error updating profile" });
            }
        }
    }
}