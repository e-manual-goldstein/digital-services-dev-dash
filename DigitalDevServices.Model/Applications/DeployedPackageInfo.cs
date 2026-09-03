namespace DigitalDevServices.Model.Applications;

public class DeployedPackageInfo
{
    public required string FileName { get; init; }

    /// <summary>
    /// Representative file path from <c>manifest.csv</c> when sourced from a manifest.
    /// </summary>
    public string? RepresentativePath { get; init; }

    public string? FileVersion { get; init; }

    public string? AssemblyVersion { get; init; }
}
