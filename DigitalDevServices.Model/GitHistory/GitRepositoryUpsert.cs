namespace DigitalDevServices.Model.GitHistory;

public class GitRepositoryUpsert
{
    public required string Name { get; set; }

    public required DateTimeOffset DateMigrated { get; set; }

    public required string CurrentLocationUrl { get; set; }
}
