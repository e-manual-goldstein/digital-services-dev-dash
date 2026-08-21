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
    }
}
