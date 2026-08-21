namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Environment details returned by the external team's Web API.
/// </summary>
public class RemoteEnvironmentDetails
{
    public int RemoteId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SqlServerInstance { get; set; } = string.Empty;
}
