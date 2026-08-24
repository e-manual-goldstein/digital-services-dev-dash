namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Application URL returned as part of remote environment details.
/// </summary>
public class EnvironmentUrl
{
    public string ApplicationName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}
