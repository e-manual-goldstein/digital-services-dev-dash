namespace DigitalDevServices.Services.Configuration;

internal static class ConfigurationFileDiscovery
{
    public static IEnumerable<string> GetConfigurationFiles(string physicalPath, string applicationName)
    {
        foreach (var filePath in GetAppsettingsFiles(physicalPath))
        {
            yield return filePath;
        }

        var webConfig = Path.Combine(physicalPath, "web.config");
        if (File.Exists(webConfig))
        {
            yield return webConfig;
        }

        var appConfig = Path.Combine(physicalPath, "app.config");
        if (File.Exists(appConfig))
        {
            yield return appConfig;
        }

        foreach (var exeConfig in GetExeConfigFiles(physicalPath, applicationName))
        {
            yield return exeConfig;
        }
    }

    private static IEnumerable<string> GetAppsettingsFiles(string physicalPath)
    {
        return Directory
            .EnumerateFiles(physicalPath, "appsettings*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => string.Equals(Path.GetFileName(path), "appsettings.json", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetExeConfigFiles(string physicalPath, string applicationName)
    {
        var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidateName in GetExeConfigCandidateNames(applicationName))
        {
            var candidatePath = Path.Combine(physicalPath, candidateName);
            if (File.Exists(candidatePath) && discovered.Add(candidatePath))
            {
                yield return candidatePath;
            }
        }

        foreach (var filePath in Directory.EnumerateFiles(physicalPath, "*.exe.config", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (discovered.Add(filePath))
            {
                yield return filePath;
            }
        }
    }

    private static IEnumerable<string> GetExeConfigCandidateNames(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            yield break;
        }

        var trimmed = applicationName.Trim();
        yield return $"{trimmed}.exe.config";

        var withoutSpaces = trimmed.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!string.Equals(withoutSpaces, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{withoutSpaces}.exe.config";
        }

        var dotted = trimmed.Replace(' ', '.');
        if (!string.Equals(dotted, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{dotted}.exe.config";
        }
    }
}
