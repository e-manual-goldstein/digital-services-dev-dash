namespace DigitalDevServices.Model.Configuration;

public class ConfigurationComparisonRow
{
    public required string Key { get; init; }

    public string? LeftValue { get; init; }

    public string? RightValue { get; init; }

    public string? LeftSource { get; init; }

    public string? RightSource { get; init; }

    public ConfigurationComparisonStatus Status { get; init; }
}
