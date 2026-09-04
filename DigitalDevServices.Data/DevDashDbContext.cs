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

    public DbSet<DeployableApplication> DeployableApplications => Set<DeployableApplication>();

    public DbSet<ApplicationInstance> ApplicationInstances => Set<ApplicationInstance>();

    public DbSet<LogFormatProfile> LogFormatProfiles => Set<LogFormatProfile>();

    public DbSet<ConfigurationSetting> ConfigurationSettings => Set<ConfigurationSetting>();

    public DbSet<GitRepository> GitRepositories => Set<GitRepository>();

    public DbSet<HistoricGitRepoRecord> HistoricGitRepoRecords => Set<HistoricGitRepoRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackedEnvironment>(entity =>
        {
            entity.ToTable("TrackedEnvironments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RemoteId).IsRequired();
            entity.HasIndex(e => e.RemoteId).IsUnique();
            entity.Property(e => e.IsFavourite).IsRequired();
            entity.Property(e => e.DisplayOrder).IsRequired();
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

        modelBuilder.Entity<DeployableApplication>(entity =>
        {
            entity.ToTable("DeployableApplications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.ProjectKey).HasMaxLength(200);
            entity.Property(e => e.IsWebApp).IsRequired();
            entity.Property(e => e.PathToLogFiles).HasMaxLength(2000);
            entity.Property(e => e.PathToPhysicalPath).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<ApplicationInstance>(entity =>
        {
            entity.ToTable("ApplicationInstances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BuildVersionNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SourceBranch).HasMaxLength(500);
            entity.Property(e => e.PhysicalPath).HasMaxLength(2000);
            entity.Property(e => e.LogPath).HasMaxLength(2000);
            entity.Property(e => e.HomepageUrl).HasMaxLength(2000);
            entity.Property(e => e.SqlServerInstance).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => new { e.DeployableApplicationId, e.EnvironmentId }).IsUnique();

            entity.HasOne(e => e.DeployableApplication)
                .WithMany()
                .HasForeignKey(e => e.DeployableApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Environment)
                .WithMany()
                .HasForeignKey(e => e.EnvironmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PipelineFeed)
                .WithMany()
                .HasForeignKey(e => e.PipelineFeedId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LogFormatProfile>(entity =>
        {
            entity.ToTable("LogFormatProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FormatName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParserConfig).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.DeployableApplicationId).IsUnique();

            entity.HasOne(e => e.DeployableApplication)
                .WithMany()
                .HasForeignKey(e => e.DeployableApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfigurationSetting>(entity =>
        {
            entity.ToTable("ConfigurationSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(500);
            entity.Property(e => e.CapturedAt).IsRequired();
            entity.HasIndex(e => new { e.ApplicationInstanceId, e.Key }).IsUnique();

            entity.HasOne(e => e.ApplicationInstance)
                .WithMany()
                .HasForeignKey(e => e.ApplicationInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GitRepository>(entity =>
        {
            entity.ToTable("GitRepositories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.DateMigrated).IsRequired();
            entity.Property(e => e.CurrentLocationUrl).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<HistoricGitRepoRecord>(entity =>
        {
            entity.ToTable("HistoricGitRepoRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LastLocationUrl).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.DateMigrated).IsRequired();

            entity.HasOne(e => e.GitRepository)
                .WithMany(repository => repository.PreviousLocations)
                .HasForeignKey(e => e.GitRepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
