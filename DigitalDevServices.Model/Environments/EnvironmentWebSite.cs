namespace DigitalDevServices.Model.Environments;

/// <summary>
/// IIS web site returned as part of remote environment details.
/// </summary>
public class EnvironmentWebSite
{
    public string? MachineName { get; set; }

    public EnvironmentWebApplication[] WebApplications { get; set; } = [];
}
