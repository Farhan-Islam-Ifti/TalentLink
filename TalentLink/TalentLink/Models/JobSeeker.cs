using System.ComponentModel.DataAnnotations;

namespace TalentLink.Models
{
    public class JobSeeker
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Skills { get; set; }

        [MaxLength(500)]
        public string? Experience { get; set; }

        [MaxLength(500)]
        public string? Education { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? CVFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ApplicationUser User { get; set; } = null!;
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
