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

    public class JobSeekerEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }

        public string? Image { get; set; }  // For displaying existing
        public IFormFile? ImageFile { get; set; } // For uploading new

        public string? CVFilePath { get; set; } // For displaying existing
        public IFormFile? CVFile { get; set; }  // For uploading new
    }

    public class AvailableJobsViewModel
    {
        public IEnumerable<JobPosting> Jobs { get; set; } = new List<JobPosting>();
        public string? Search { get; set; }
        public string? Location { get; set; }
        public JobType? JobType { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

}
