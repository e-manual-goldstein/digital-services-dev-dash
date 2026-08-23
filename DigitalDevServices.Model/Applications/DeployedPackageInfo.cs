namespace DigitalDevServices.Model.Applications;

public class DeployedPackageInfo
{
    public required string FileName { get; init; }

    public string? FileVersion { get; init; }

    public string? AssemblyVersion { get; init; }
}
