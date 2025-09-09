using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TalentLink.Models;

namespace TalentLink.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<JobSeeker> JobSeekers { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Interview> Interviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Company relationships
            builder.Entity<Company>()
                .HasOne(c => c.User)
                .WithOne(u => u.Company)
                .HasForeignKey<Company>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Explicitly set cascade delete

            // JobSeeker relationships
            builder.Entity<JobSeeker>()
                .HasOne(js => js.User)
                .WithOne(u => u.JobSeeker)
                .HasForeignKey<JobSeeker>(js => js.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Explicitly set cascade delete

            // JobPosting relationships
            builder.Entity<JobPosting>()
                .HasOne(jp => jp.Company)
                .WithMany(c => c.JobPostings)
                .HasForeignKey(jp => jp.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete when company is deleted

            // JobApplication relationships - FIXED: Use DeleteBehavior.Restrict or DeleteBehavior.ClientSetNull
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPosting)
                .WithMany(jp => jp.JobApplications)
                .HasForeignKey(ja => ja.JobPostingId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to Restrict

            builder.Entity<JobApplication>()
                .HasOne(ja => ja.JobSeeker)
                .WithMany(js => js.JobApplications)
                .HasForeignKey(ja => ja.JobSeekerId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to Restrict

            builder.Entity<JobApplication>()
                .HasOne(ja => ja.Specialist)
                .WithMany()
                .HasForeignKey(ja => ja.SpecialistId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); // Set null when specialist is deleted

            // Interview relationships
            builder.Entity<Interview>()
                .HasOne(i => i.JobApplication)
                .WithMany(ja => ja.Interviews)
                .HasForeignKey(i => i.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete when application is deleted

            builder.Entity<Interview>()
                .HasOne(i => i.Specialist)
                .WithMany()
                .HasForeignKey(i => i.SpecialistId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict delete when specialist is referenced

            // Configure decimal precision
            builder.Entity<JobPosting>()
                .Property(jp => jp.Salary)
                .HasPrecision(18, 2);

            // Additional configuration to avoid circular references
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPosting)
                .WithMany(jp => jp.JobApplications)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<JobApplication>()
                .HasOne(ja => ja.JobSeeker)
                .WithMany(js => js.JobApplications)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}