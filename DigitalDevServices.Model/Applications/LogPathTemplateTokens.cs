namespace DigitalDevServices.Model.Applications;

/// <summary>
/// Token names supported in <see cref="Entities.DeployableApplication.PathToLogFiles"/> templates.
/// </summary>
public static class LogPathTemplateTokens
{
    public const string AppName = "AppName";
    public const string EnvironmentCode = "EnvironmentCode";
    public const string EnvironmentName = "EnvironmentName";
    public const string MachineName = "MachineName";
    public const string ApplicationPoolName = "ApplicationPoolName";
    public const string VirtualPath = "VirtualPath";
    public const string PhysicalPath = "PhysicalPath";

    public static IReadOnlyList<string> All { get; } =
    [
        AppName,
        EnvironmentCode,
        EnvironmentName,
        MachineName,
        ApplicationPoolName,
        VirtualPath,
        PhysicalPath
    ];
}
