using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TalentLink.Models
{
    public class UpdateProfileModel
    {
        // Basic user info
        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        // Company specific fields
        [MaxLength(100)]
        public string? CompanyName { get; set; }

        [MaxLength(50)]
        public string? Industry { get; set; }

        [Url]
        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // JobSeeker specific fields
        [MaxLength(500)]
        public string? Skills { get; set; }

        [MaxLength(500)]
        public string? Experience { get; set; }

        [MaxLength(500)]
        public string? Education { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        // CV file upload
        public IFormFile? CVFile { get; set; }
    }
}