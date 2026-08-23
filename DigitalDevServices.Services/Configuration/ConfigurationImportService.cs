using System.Text.Json;
using DigitalDevServices.Model.Configuration;
using DigitalDevServices.Services.Applications;

namespace DigitalDevServices.Services.Configuration;

public sealed class ConfigurationImportService : IConfigurationImportService
{
    private readonly IApplicationInstanceService _applicationInstanceService;
    private readonly IConfigurationSettingService _configurationSettingService;

    public ConfigurationImportService(
        IApplicationInstanceService applicationInstanceService,
        IConfigurationSettingService configurationSettingService)
    {
        _applicationInstanceService = applicationInstanceService;
        _configurationSettingService = configurationSettingService;
    }

    public async Task<ConfigurationImportResult> RefreshAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await _applicationInstanceService
            .GetByIdAsync(applicationInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            return new ConfigurationImportResult
            {
                ErrorMessage = "Application instance was not found."
            };
        }

        var physicalPath = instance.PhysicalPath?.Trim();
        if (string.IsNullOrWhiteSpace(physicalPath))
        {
            return new ConfigurationImportResult
            {
                ErrorMessage = "No physical path is configured for this deployment."
            };
        }

        if (!Directory.Exists(physicalPath))
        {
            return new ConfigurationImportResult
            {
                ErrorMessage = $"Deploy folder does not exist: {physicalPath}"
            };
        }

        var mergedSettings = new Dictionary<string, (string Value, string Source)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var filePath in GetAppsettingsFiles(physicalPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(filePath);
                string jsonContent;

                try
                {
                    jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return new ConfigurationImportResult
                    {
                        ErrorMessage = $"Could not read '{fileName}': {ex.Message}"
                    };
                }

                IReadOnlyDictionary<string, string> flattened;

                try
                {
                    flattened = JsonConfigurationFlattener.Flatten(jsonContent);
                }
                catch (JsonException ex)
                {
                    return new ConfigurationImportResult
                    {
                        ErrorMessage = $"Could not parse '{fileName}': {ex.Message}"
                    };
                }

                foreach (var (key, value) in flattened)
                {
                    mergedSettings[key] = (value, fileName);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ConfigurationImportResult
            {
                ErrorMessage = $"Could not read configuration from '{physicalPath}': {ex.Message}"
            };
        }

        var upserts = mergedSettings
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ConfigurationSettingUpsert
            {
                ApplicationInstanceId = applicationInstanceId,
                Key = pair.Key,
                Value = pair.Value.Value,
                Source = pair.Value.Source
            })
            .ToList();

        var settings = await _configurationSettingService
            .UpsertManyAsync(applicationInstanceId, upserts, cancellationToken)
            .ConfigureAwait(false);

        return new ConfigurationImportResult
        {
            Settings = settings
        };
    }

    private static IEnumerable<string> GetAppsettingsFiles(string physicalPath)
    {
        return Directory
            .EnumerateFiles(physicalPath, "appsettings*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => string.Equals(Path.GetFileName(path), "appsettings.json", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);
    }
}
