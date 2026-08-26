using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalDevServices.Model.Environments;

public class RemoteBuildVersionDetails
{
    // (2) WorkItemBuildNumber — TFS work item id echoed from GetBuildVersionDetails.
    // Proposed rename: WorkItemBuildNumber
    public string? BuildNumber { get; set; }

    public string? FromShaId { get; set; }

    public string? Project { get; set; }

    public string? SourceBranch { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    private static readonly JsonSerializerOptions PrettyJsonOptions = new() { WriteIndented = true };

    public bool HasAdditionalProperties => AdditionalProperties is { Count: > 0 };

    public string? FormatAdditionalPropertiesJson()
    {
        if (!HasAdditionalProperties)
        {
            return null;
        }

        return JsonSerializer.Serialize(AdditionalProperties, PrettyJsonOptions);
    }
}
