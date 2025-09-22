using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TalentLink.Data;
using TalentLink.Services;

namespace TalentLink.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class RecommendationController : Controller
    {
        private readonly IJobRecommendationService _reco;
        private readonly ApplicationDbContext _db;

        public RecommendationController(IJobRecommendationService reco, ApplicationDbContext db)
        {
            _reco = reco;
            _db = db;
        }

        // Returns a ready-to-render HTML partial (recommended jobs card list)
        [HttpGet]
        public async Task<IActionResult> Recommended(int limit = 6)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var jobs = await _reco.GetForUserAsync(userId, limit);

            // pull user's applications to show "View Application" link if needed
            var seeker = await _db.JobSeekers.AsNoTracking().FirstOrDefaultAsync(js => js.UserId == userId);
            var apps = await _db.JobApplications
                .AsNoTracking()
                .Where(a => a.JobSeekerId == seeker!.Id)
                .ToListAsync();

            var vm = new RecommendedJobsVM
            {
                Jobs = jobs,
                ApplicationsByJobId = apps.ToDictionary(a => a.JobPostingId, a => a.Id)
            };

            // Partial view below
            return PartialView("~/Views/Dashboard/_RecommendedJobs.cshtml", vm);
        }
    }

    public class RecommendedJobsVM
    {
        public List<TalentLink.Models.JobPosting> Jobs { get; set; } = new();
        public Dictionary<int, int> ApplicationsByJobId { get; set; } = new(); // JobPostingId -> ApplicationId
    }
}
