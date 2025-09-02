using System;
using System.Collections.Generic;

namespace TalentLink.Models
{
    public class JobPosting
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeadlineDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public Company Company { get; set; }
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
