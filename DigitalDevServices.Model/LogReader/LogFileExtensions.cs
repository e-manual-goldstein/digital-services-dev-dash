namespace DigitalDevServices.Model.Logs;

/// <summary>
/// File extensions treated as log files when listing or validating log paths.
/// </summary>
public static class LogFileExtensions
{
    public static readonly string[] All = [".log", ".txt"];

    public static bool IsSupported(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath.Trim());
        return All.Any(supported => extension.Equals(supported, StringComparison.OrdinalIgnoreCase));
    }

    public static string Description => string.Join(" or ", All);
}
