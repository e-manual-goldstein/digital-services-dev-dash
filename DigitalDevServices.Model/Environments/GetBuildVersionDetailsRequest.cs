namespace DigitalDevServices.Model.Environments;

public class GetBuildVersionDetailsRequest
{
    // (2) WorkItemBuildNumber — string form of the TFS work item id sent to GetBuildVersionDetails.
    // Proposed rename: WorkItemBuildNumber
    public required string BuildNumber { get; init; }

    public bool IncludeVersionControlLog { get; init; } = true;
}
