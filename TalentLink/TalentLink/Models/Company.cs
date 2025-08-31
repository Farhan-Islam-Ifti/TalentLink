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
}
