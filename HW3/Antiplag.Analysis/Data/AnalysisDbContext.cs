using Antiplag.Analysis.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Antiplag.Analysis.Data;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : base(options)
    {
    }

    public DbSet<SubmissionEntity> Submissions => Set<SubmissionEntity>();
    public DbSet<ReportEntity> Reports => Set<ReportEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SubmissionEntity>(entity =>
        {
            entity.ToTable("submissions");
            entity.HasKey(x => x.SubmissionId);

            entity.Property(x => x.WorkId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.StudentId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Hash).IsRequired().HasMaxLength(64);

            entity.Property(x => x.Shingles).HasColumnType("text[]");

            entity.HasIndex(x => new { x.WorkId, x.CreatedAt });
            entity.HasIndex(x => new { x.WorkId, x.StudentId });
        });

        modelBuilder.Entity<ReportEntity>(entity =>
        {
            entity.ToTable("reports");
            entity.HasKey(x => x.ReportId);

            entity.HasOne(x => x.Submission)
                .WithMany(s => s.Reports)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.SubmissionId);
            entity.HasIndex(x => x.CreatedAt);
        });
    }
}
