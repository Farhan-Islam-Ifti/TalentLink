using Microsoft.AspNetCore.Identity;
using TalentLink.Models;

namespace TalentLink.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterModel model);
        Task<bool> ValidateLoginAsync(LoginModel model); // Renamed for clarity
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role.ToString());
            }

            return result;
        }

        public async Task<bool> ValidateLoginAsync(LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return false;

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            return result.Succeeded;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }
    }
}