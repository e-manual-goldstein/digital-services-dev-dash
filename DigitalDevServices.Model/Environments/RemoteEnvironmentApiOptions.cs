namespace DigitalDevServices.Model.Environments;

public class RemoteEnvironmentApiOptions
{
    public const string SectionName = "RemoteEnvironmentApi";

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Relative path template for a single environment. Use {id} for the remote id.
    /// </summary>
    public string GetEnvironmentPath { get; set; } = "api/environments/{id}";

    /// <summary>
    /// Relative path for listing all environments.
    /// </summary>
    public string ListEnvironmentsPath { get; set; } = "api/environments";
}
