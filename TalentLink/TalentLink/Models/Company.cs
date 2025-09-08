using System.Collections.Generic;

namespace TalentLink.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser User { get; set; }
        public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }

    public class CompanyEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;  // from ApplicationUser
        public string LastName { get; set; } = string.Empty;   // from ApplicationUser
        public string PhoneNumber { get; set; } = string.Empty; // from IdentityUser (base)

        public string? CompanyName { get; set; }   // from Company
        public string? Industry { get; set; }      // from Company
        public string? Website { get; set; }       // from Company
        public string? Address { get; set; }       // from Company
        public string? Description { get; set; }   // from Company

        public string? Image { get; set; }         // from ApplicationUser
        public IFormFile? ImageFile { get; set; }  // for uploads
    }

}
