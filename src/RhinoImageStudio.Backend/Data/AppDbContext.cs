using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Data;

/// <summary>
/// Entity Framework Core database context for Rhino Image Studio
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Capture> Captures => Set<Capture>();
    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<ReferenceImage> ReferenceImages => Set<ReferenceImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsPinned);
        });

        // Capture
        modelBuilder.Entity<Capture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
            entity.Property(e => e.ViewName).HasMaxLength(100);
            entity.Property(e => e.CameraPosition).HasMaxLength(100);
            entity.Property(e => e.CameraTarget).HasMaxLength(100);

            entity.HasOne(e => e.Project)
                .WithMany(s => s.Captures)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Generation
        modelBuilder.Entity<Generation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Prompt).HasMaxLength(2000);
            entity.Property(e => e.NegativePrompt).HasMaxLength(1000);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
            entity.Property(e => e.FalRequestId).HasMaxLength(100);
            entity.Property(e => e.ModelId).HasMaxLength(100);

            entity.HasOne(e => e.Project)
                .WithMany(s => s.Generations)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourceCapture)
                .WithMany()
                .HasForeignKey(e => e.SourceCaptureId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ParentGeneration)
                .WithMany(g => g.ChildGenerations)
                .HasForeignKey(e => e.ParentGenerationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ParentGenerationId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.ProjectId, e.IsArchived });
        });

        // ReferenceImage
        modelBuilder.Entity<ReferenceImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ThumbnailPath).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.References)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
        });

        // Job
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestJson).IsRequired();
            entity.Property(e => e.ProgressMessage).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.FalRequestId).HasMaxLength(100);
            entity.Property(e => e.ProviderModelId).HasMaxLength(100);

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ResultId);
        });
    }
}
