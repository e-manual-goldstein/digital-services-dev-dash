namespace DigitalDevServices.Model.GitHistory;

public class ArtifactComponentUpsert
{
    public required string Name { get; set; }

    public required DateTimeOffset DateMigrated { get; set; }

    public required string CurrentLocationUrl { get; set; }
}
