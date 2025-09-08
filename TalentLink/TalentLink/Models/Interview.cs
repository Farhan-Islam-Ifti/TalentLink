namespace TalentLink.Models
{
    public class Interview
    {
        public int Id { get; set; }
        public int JobApplicationId { get; set; }
        public string SpecialistId { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? MeetingLink { get; set; }
        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
        public string? Notes { get; set; }
        public string? Feedback { get; set; }
        public int? Rating { get; set; } // 1-5 scale
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public JobApplication JobApplication { get; set; }
        public ApplicationUser Specialist { get; set; }
    }
}