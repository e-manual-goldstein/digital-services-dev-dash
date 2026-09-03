using DigitalDevServices.Model.Applications;

namespace DigitalDevServices.Services.Applications;

public static class DeployedPackageComparer
{
    public static IReadOnlyList<DeployedPackageComparisonRow> Compare(
        DeployedPackageScanResult leftScan,
        DeployedPackageScanResult rightScan)
    {
        var leftPackages = IndexPackages(leftScan.Packages);
        var rightPackages = IndexPackages(rightScan.Packages);
        var fileNames = leftPackages.Keys
            .Union(rightPackages.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<DeployedPackageComparisonRow>(fileNames.Count);

        foreach (var fileName in fileNames)
        {
            leftPackages.TryGetValue(fileName, out var leftPackage);
            rightPackages.TryGetValue(fileName, out var rightPackage);

            var leftVersion = leftPackage is null ? null : DeployedPackageVersionFormatter.GetDisplayVersion(leftPackage);
            var rightVersion = rightPackage is null ? null : DeployedPackageVersionFormatter.GetDisplayVersion(rightPackage);

            rows.Add(new DeployedPackageComparisonRow
            {
                FileName = fileName,
                LeftVersion = leftVersion,
                RightVersion = rightVersion,
                Status = ResolveStatus(leftPackage, rightPackage, leftVersion, rightVersion)
            });
        }

        return rows;
    }

    private static Dictionary<string, DeployedPackageInfo> IndexPackages(
        IReadOnlyList<DeployedPackageInfo> packages)
    {
        var index = new Dictionary<string, DeployedPackageInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in packages)
        {
            index[package.FileName] = package;
        }

        return index;
    }

    private static DeployedPackageComparisonStatus ResolveStatus(
        DeployedPackageInfo? leftPackage,
        DeployedPackageInfo? rightPackage,
        string? leftVersion,
        string? rightVersion)
    {
        if (leftPackage is null)
        {
            return DeployedPackageComparisonStatus.RightOnly;
        }

        if (rightPackage is null)
        {
            return DeployedPackageComparisonStatus.LeftOnly;
        }

        if (string.Equals(leftVersion, rightVersion, StringComparison.OrdinalIgnoreCase))
        {
            return DeployedPackageComparisonStatus.Match;
        }

        return DeployedPackageComparisonStatus.Mismatch;
    }
}
