using System;

namespace TalentLink.Models
{
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

        // Navigation
        public JobApplication JobApplication { get; set; }
        public ApplicationUser Specialist { get; set; }
    }
}
