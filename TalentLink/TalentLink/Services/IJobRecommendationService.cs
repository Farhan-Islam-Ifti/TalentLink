using TalentLink.Models;

namespace TalentLink.Services
{
    public interface IJobRecommendationService
    {
        Task<List<JobPosting>> GetForUserAsync(string userId, int limit = 6, CancellationToken ct = default);
    }
}
