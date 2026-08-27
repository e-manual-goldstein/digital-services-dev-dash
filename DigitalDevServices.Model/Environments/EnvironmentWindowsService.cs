namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Windows service returned as part of remote environment details.
/// </summary>
public class EnvironmentWindowsService
{
    public string? MachineName { get; set; }

    public string? DisplayName { get; set; }

    public string? BinaryPathName { get; set; }

    /// <summary>
    /// Resolves the deployable application name from <see cref="DisplayName"/>, or the
    /// executable file name (without extension) from <see cref="BinaryPathName"/> when display name is empty.
    /// </summary>
    public string? ResolveDeployableApplicationName()
    {
        var displayName = DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return ExtractExecutableNameWithoutExtension(BinaryPathName);
    }

    private static string? ExtractExecutableNameWithoutExtension(string? binaryPath)
    {
        if (string.IsNullOrWhiteSpace(binaryPath))
        {
            return null;
        }

        var fileName = Path.GetFileName(binaryPath.Trim().Trim('"'));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(nameWithoutExtension) ? null : nameWithoutExtension;
    }
}
