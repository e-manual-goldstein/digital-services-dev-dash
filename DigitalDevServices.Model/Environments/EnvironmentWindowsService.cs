namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Windows service returned as part of remote environment details.
/// </summary>
public class EnvironmentWindowsService
{
    public string? MachineName { get; set; }

    public string? DisplayName { get; set; }

    public string? BinaryPathName { get; set; }
}
