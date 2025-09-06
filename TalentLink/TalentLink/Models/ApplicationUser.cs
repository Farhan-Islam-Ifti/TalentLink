using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Design;
using System.ComponentModel.DataAnnotations;
using System;

namespace TalentLink.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string? Image { get; set; }
        // Navigation
        public Company? Company { get; set; }
        public JobSeeker? JobSeeker { get; set; }
    }
}
