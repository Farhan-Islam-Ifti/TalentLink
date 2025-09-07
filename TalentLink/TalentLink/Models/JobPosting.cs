using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentLink.Models
{
    public class JobPosting
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string Requirements { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Salary { get; set; }

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        public JobType JobType { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        public DateTime? DeadlineDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Company Company { get; set; } = null!;
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }

   
}