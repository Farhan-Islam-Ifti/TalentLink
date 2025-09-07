using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TalentLink.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Company? Company { get; set; }
        public JobSeeker? JobSeeker { get; set; }
    }

    public enum UserRole
    {
        Admin,
        Company,
        JobSeeker,
        Specialist
    }

    public class Company
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigation properties
        public ApplicationUser User { get; set; }
        public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }

    public class JobSeeker
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string CVFilePath { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;

        // Navigation properties
        public ApplicationUser User { get; set; }
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }

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

        // Navigation properties
        public Company Company { get; set; }
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }

    public enum JobType
    {
        FullTime,
        PartTime,
        Contract,
        Internship
    }

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

        // Navigation properties
        public JobPosting JobPosting { get; set; }
        public JobSeeker JobSeeker { get; set; }
        public ApplicationUser? Specialist { get; set; }
        public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    }

    public enum ApplicationStatus
    {
        Submitted,
        UnderReview,
        InterviewScheduled,
        Interviewed,
        Accepted,
        Rejected
    }

    public class Interview
    {
        public int Id { get; set; }
        public int JobApplicationId { get; set; }
        public string SpecialistId { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string MeetingRoom { get; set; } = string.Empty;
        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
        public string? Notes { get; set; }
        public int? Rating { get; set; }
        public string? Feedback { get; set; }

        // Navigation properties
        public JobApplication JobApplication { get; set; }
        public ApplicationUser Specialist { get; set; }
    }

    public enum InterviewStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled
    }
}