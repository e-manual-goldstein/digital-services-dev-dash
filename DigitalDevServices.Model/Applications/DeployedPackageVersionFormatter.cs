namespace DigitalDevServices.Model.Applications;

public static class DeployedPackageVersionFormatter
{
    public static string? GetDisplayVersion(DeployedPackageInfo package)
    {
        if (!string.IsNullOrWhiteSpace(package.AssemblyVersion))
        {
            return package.AssemblyVersion.Trim();
        }

        if (!string.IsNullOrWhiteSpace(package.FileVersion))
        {
            return package.FileVersion.Trim();
        }

        return null;
    }
}
