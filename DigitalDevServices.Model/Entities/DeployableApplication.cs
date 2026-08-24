namespace DigitalDevServices.Model.Entities;

/// <summary>
/// A compilable/deployable project in the wider codebase — independent of environment.
/// </summary>
public class DeployableApplication
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ProjectKey { get; set; }

    public bool IsWebApp { get; set; }

    public string? PathToLogFiles { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
