namespace DigitalDevServices.Model.GitHistory;

public class HistoricGitRepoRecordUpsert
{
    public required string Name { get; set; }

    public required string LastLocationUrl { get; set; }

    public required DateTimeOffset DateMigrated { get; set; }
}
