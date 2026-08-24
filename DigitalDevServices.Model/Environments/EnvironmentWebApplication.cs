namespace DigitalDevServices.Model.Environments;

/// <summary>
/// IIS web application returned as part of remote environment details.
/// </summary>
public class EnvironmentWebApplication
{
    public string? ApplicationPoolName { get; set; }

    public string? Path { get; set; }

    public string? PhysicalPath { get; set; }

    /// <summary>
    /// Resolves the deployable application name: last segment of <see cref="Path"/>, or
    /// <see cref="ApplicationPoolName"/> when path is empty.
    /// </summary>
    public string? ResolveDeployableApplicationName()
    {
        var pathSegment = ExtractLastPathSegment(Path);
        if (!string.IsNullOrWhiteSpace(pathSegment))
        {
            return pathSegment;
        }

        var poolName = ApplicationPoolName?.Trim();
        return string.IsNullOrWhiteSpace(poolName) ? null : poolName;
    }

    private static string? ExtractLastPathSegment(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmed = path.Trim().TrimEnd('/');
        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? null : segments[^1];
    }
}
