namespace DigitalDevServices.Model.Configuration;

public class ConfigurationComparisonResult
{
    public Guid LeftInstanceId { get; init; }

    public Guid RightInstanceId { get; init; }

    public IReadOnlyList<ConfigurationComparisonRow> Rows { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
