using System.ComponentModel.DataAnnotations;

namespace TalentLink.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Industry { get; set; } = string.Empty;

        [Url]
        [MaxLength(200)]
        public string Website { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ApplicationUser User { get; set; } = null!;
        public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}