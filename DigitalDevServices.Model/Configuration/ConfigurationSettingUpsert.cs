namespace DigitalDevServices.Model.Configuration;

public class ConfigurationSettingUpsert
{
    public required Guid ApplicationInstanceId { get; init; }

    public required string Key { get; init; }

    public required string Value { get; init; }

    public string? Source { get; init; }
}
