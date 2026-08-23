namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A captured configuration key/value for a deployed <see cref="ApplicationInstance"/>.
/// </summary>
public class ConfigurationSetting
{
    public Guid Id { get; set; }

    public Guid ApplicationInstanceId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Source { get; set; }

    public DateTimeOffset CapturedAt { get; set; }

    public ApplicationInstance ApplicationInstance { get; set; } = null!;
}
