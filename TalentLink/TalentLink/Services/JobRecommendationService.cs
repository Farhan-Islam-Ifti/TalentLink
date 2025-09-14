using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TalentLink.Data;
using TalentLink.Models;

namespace TalentLink.Services
{
    public class JobRecommendationService : IJobRecommendationService
    {
        private readonly ApplicationDbContext _db;

        public JobRecommendationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<JobPosting>> GetForUserAsync(string userId, int limit = 6, CancellationToken ct = default)
        {
            // Get seeker (skills, address as a coarse location)
            var seeker = await _db.JobSeekers
                .AsNoTracking()
                .FirstOrDefaultAsync(js => js.UserId == userId, ct);
            if (seeker == null) return new List<JobPosting>();

            // Jobs user already applied to
            var appliedIds = await _db.JobApplications
                .Where(a => a.JobSeeker.UserId == userId)
                .Select(a => a.JobPostingId)
                .ToListAsync(ct);

            // Base candidate set: recent, active, not already applied
            var candidates = await _db.JobPostings
                .AsNoTracking()
                .Include(j => j.Company)
                .Where(j => j.IsActive && !appliedIds.Contains(j.Id))
                .OrderByDescending(j => j.PostedDate)
                .Take(250) // keep EF query light; we’ll score in memory
                .ToListAsync(ct);

            // Tokenize seeker skills (split on commas/whitespace)
            var skillTokens = new HashSet<string>(StringSplit((seeker.Skills ?? "").ToLower()));

            // Simple scoring: skills overlap + location match + recentness
            var scored = candidates
                .Select(j =>
                {
                    var text = $"{j.Title} {j.Description} {j.Requirements}".ToLower();
                    var tokens = new HashSet<string>(StringSplit(text));

                    int skillHits = skillTokens.Count == 0 ? 0 : skillTokens.Intersect(tokens).Count();
                    int locationHit = (!string.IsNullOrWhiteSpace(seeker.Address) && !string.IsNullOrWhiteSpace(j.Location) &&
                                       j.Location.Contains(seeker.Address, StringComparison.OrdinalIgnoreCase)) ? 2 : 0;

                    double daysOld = (DateTime.UtcNow - j.PostedDate).TotalDays;
                    daysOld = Math.Clamp(daysOld, 0, 30);
                    double recentBonus = 3.0 - (daysOld / 10.0);

                    double score = (skillHits * 3) + locationHit + recentBonus;
                    return new { Job = j, Score = score };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Job.PostedDate)
                .Take(limit)
                .Select(x => x.Job)
                .ToList();

            return scored;

            static IEnumerable<string> StringSplit(string text)
            {
                // split on non-letters/numbers and commas
                foreach (var t in Regex.Split(text, @"[^a-z0-9\+#]+"))
                {
                    var v = t.Trim();
                    if (!string.IsNullOrWhiteSpace(v)) yield return v;
                }
            }
        }
    }
}
