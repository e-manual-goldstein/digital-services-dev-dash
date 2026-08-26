namespace DigitalDevServices.Model.Applications;

/// <summary>
/// Values available when resolving <see cref="DeployableApplication.PathToLogFiles"/> or
/// <see cref="DeployableApplication.PathToPhysicalPath"/> templates.
/// </summary>
public sealed class LogPathTemplateContext
{
    public string? AppName { get; init; }

    public string? EnvironmentCode { get; init; }

    public string? EnvironmentName { get; init; }

    public string? MachineName { get; init; }

    public string? ApplicationPoolName { get; init; }

    public string? VirtualPath { get; init; }

    public string? PhysicalPath { get; init; }
}
