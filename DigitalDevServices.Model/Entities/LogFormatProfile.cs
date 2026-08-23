namespace DigitalDevServices.Model.Entities;

/// <summary>
/// Log parsing profile for a <see cref="DeployableApplication"/> — same format across all environments.
/// </summary>
public class LogFormatProfile
{
    public Guid Id { get; set; }

    public Guid DeployableApplicationId { get; set; }

    public string FormatName { get; set; } = string.Empty;

    public string ParserConfig { get; set; } = "{}";

    public string? Notes { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DeployableApplication DeployableApplication { get; set; } = null!;
}
