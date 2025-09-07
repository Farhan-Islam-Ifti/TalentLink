using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TalentLink.Models
{
    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        public UserRole Role { get; set; }

        // Company specific fields
        [RequiredIf(nameof(Role), UserRole.Company, ErrorMessage = "Company name is required")]
        [MaxLength(100)]
        public string? CompanyName { get; set; }

        [MaxLength(50)]
        public string? Industry { get; set; }

        [Url]
        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(200)]
        public string? CompanyAddress { get; set; }

        [MaxLength(500)]
        public string? CompanyDescription { get; set; }

        // JobSeeker specific fields
        [MaxLength(500)]
        public string? Skills { get; set; }

        [MaxLength(500)]
        public string? Experience { get; set; }

        [MaxLength(500)]
        public string? Education { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        // File upload
        public IFormFile? CVFile { get; set; }
    }

    // Custom validation attribute for conditional validation
    public class RequiredIfAttribute : ValidationAttribute
    {
        private string PropertyName { get; set; }
        private object DesiredValue { get; set; }

        public RequiredIfAttribute(string propertyName, object desiredValue, string errorMessage = "")
        {
            PropertyName = propertyName;
            DesiredValue = desiredValue;
            ErrorMessage = errorMessage;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();
            var propertyValue = type.GetProperty(PropertyName)?.GetValue(instance, null);

            if (propertyValue?.Equals(DesiredValue) == true)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}