using DigitalDevServices.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Data;

public class DevDashDbContext : DbContext
{
    public DevDashDbContext(DbContextOptions<DevDashDbContext> options)
        : base(options)
    {
    }

    public DbSet<TrackedEnvironment> TrackedEnvironments => Set<TrackedEnvironment>();

    public DbSet<PipelineFeed> PipelineFeeds => Set<PipelineFeed>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackedEnvironment>(entity =>
        {
            entity.ToTable("TrackedEnvironments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RemoteId).IsRequired();
            entity.HasIndex(e => e.RemoteId).IsUnique();
            entity.Property(e => e.DateLastUpdated).IsRequired();
        });

        modelBuilder.Entity<PipelineFeed>(entity =>
        {
            entity.ToTable("PipelineFeeds");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
