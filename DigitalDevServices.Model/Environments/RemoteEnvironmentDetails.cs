namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Environment details returned by the external team's Web API.
/// </summary>
public class RemoteEnvironmentDetails
{
    public string Code { get; set; } = string.Empty;

    public string EnvironmentType { get; set; } = string.Empty;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
