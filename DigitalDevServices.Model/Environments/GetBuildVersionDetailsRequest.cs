namespace DigitalDevServices.Model.Environments;

public class GetBuildVersionDetailsRequest
{
    public required string BuildNumber { get; init; }

    public bool IncludeVersionControlLog { get; init; } = true;
}
