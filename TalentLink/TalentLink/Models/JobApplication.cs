using System;
using System.Collections.Generic;

namespace TalentLink.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobPostingId { get; set; }
        public int JobSeekerId { get; set; }
        public string CoverLetter { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        public DateTime? InterviewDate { get; set; }
        public string? InterviewNotes { get; set; }
        public string? SpecialistId { get; set; }

        // Navigation
        public JobPosting JobPosting { get; set; }
        public JobSeeker JobSeeker { get; set; }
        public ApplicationUser? Specialist { get; set; }
        public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    }
}
