namespace DigitalDevServices.Model.Environments;

/// <summary>
/// IIS web site returned as part of remote environment details.
/// </summary>
public class EnvironmentWebSite
{
    public string? Name { get; set; }

    public string? MachineName { get; set; }

    public EnvironmentWebApplication[] WebApplications { get; set; } = [];

    public string FormatSectionTitle()
    {
        var name = string.IsNullOrWhiteSpace(Name) ? "Unknown site" : Name.Trim();
        var machineName = string.IsNullOrWhiteSpace(MachineName) ? "Unknown machine" : MachineName.Trim();
        return $"{name} - {machineName}";
    }
}
