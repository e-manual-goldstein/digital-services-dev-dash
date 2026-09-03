using DigitalDevServices.Data;
using DigitalDevServices.Model.Environments;
using Microsoft.EntityFrameworkCore;

namespace DigitalDevServices.Services.Environments;

public sealed class EnvironmentInstanceSnapshotSyncService : IEnvironmentInstanceSnapshotSyncService
{
    private static readonly string[] DeployedDateParameterNames =
    [
        "DeployedDate",
        "DeploymentDate",
        "deployedAt"
    ];

    private readonly DevDashDbContext _db;

    public EnvironmentInstanceSnapshotSyncService(DevDashDbContext db)
    {
        _db = db;
    }

    public async Task<int> SyncInstancesAsync(
        Guid environmentLocalId,
        EnvironmentRefreshSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var instances = await _db.ApplicationInstances
            .Include(instance => instance.DeployableApplication)
            .Where(instance => instance.EnvironmentId == environmentLocalId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (instances.Count == 0)
        {
            return 0;
        }

        snapshot.Details.TryGetAdditionalString("SqlServerInstance", out var environmentSqlServerInstance);
        var updatedCount = 0;

        foreach (var instance in instances)
        {
            if (ApplySnapshotToInstance(instance, snapshot, environmentSqlServerInstance))
            {
                instance.UpdatedAt = snapshot.DateLastRefreshed;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return updatedCount;
    }

    internal static bool ApplySnapshotToInstance(
        Model.Entities.ApplicationInstance instance,
        EnvironmentRefreshSnapshot snapshot,
        string? environmentSqlServerInstance)
    {
        var applicationName = instance.DeployableApplication.Name;
        var build = snapshot.DeploymentDetails?.GetBuildForApplication(applicationName);
        var pipelineBuildNumber = build?.EnvironmentPipelineBuildNumber.ToString();
        snapshot.BuildVersionDetailsByBuildNumber.TryGetValue(
            pipelineBuildNumber ?? string.Empty,
            out var buildVersionDetails);

        var changed = false;

        var suggestedBuildVersionNumber = build?.BuildVersionNumber;
        if (!string.IsNullOrWhiteSpace(suggestedBuildVersionNumber)
            && !string.Equals(instance.BuildVersionNumber, suggestedBuildVersionNumber, StringComparison.OrdinalIgnoreCase))
        {
            instance.BuildVersionNumber = suggestedBuildVersionNumber;
            instance.DeployedAt ??= snapshot.DateLastRefreshed;
            changed = true;
        }

        var sourceBranch = buildVersionDetails?.SourceBranch
            ?? snapshot.DeploymentDetails?.GetWipBranchForApplication(applicationName)
            ?? snapshot.DeploymentDetails?.GetPrimaryWipBranch();
        if (!string.IsNullOrWhiteSpace(sourceBranch)
            && !string.Equals(instance.SourceBranch, sourceBranch, StringComparison.OrdinalIgnoreCase))
        {
            instance.SourceBranch = sourceBranch;
            changed = true;
        }

        var deployedAt = TryParseDeployedAt(build);
        if (deployedAt.HasValue && instance.DeployedAt != deployedAt)
        {
            instance.DeployedAt = deployedAt;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(environmentSqlServerInstance)
            && !string.Equals(instance.SqlServerInstance, environmentSqlServerInstance, StringComparison.OrdinalIgnoreCase))
        {
            instance.SqlServerInstance = environmentSqlServerInstance;
            changed = true;
        }

        return changed;
    }

    private static DateTimeOffset? TryParseDeployedAt(EnvironmentBuild? build)
    {
        if (build is null)
        {
            return null;
        }

        foreach (var parameterName in DeployedDateParameterNames)
        {
            var value = build.TryGetParameterValue(parameterName);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (DateTimeOffset.TryParse(value, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
