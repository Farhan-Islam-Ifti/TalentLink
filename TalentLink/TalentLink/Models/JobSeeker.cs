using System;
using System.Collections.Generic;

namespace TalentLink.Models
{
    public class JobSeeker
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string? CVFilePath { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser User { get; set; }
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }

    public class JobSeekerEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }

        public string? Image { get; set; }  // For displaying existing
        public IFormFile? ImageFile { get; set; } // For uploading new

        public string? CVFilePath { get; set; } // For displaying existing
        public IFormFile? CVFile { get; set; }  // For uploading new
    }

}
