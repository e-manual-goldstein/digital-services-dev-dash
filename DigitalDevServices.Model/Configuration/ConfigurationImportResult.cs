using DigitalDevServices.Model.Entities;

namespace DigitalDevServices.Model.Configuration;

public class ConfigurationImportResult
{
    public IReadOnlyList<ConfigurationSetting> Settings { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
