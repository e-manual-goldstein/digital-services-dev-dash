using System.Text.Json;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.MockRemoteApi;

internal static class SampleBuildVersionDetails
{
    public static RemoteBuildVersionDetails? ForBuildNumber(string buildNumber)
    {
        if (!buildNumber.Equals("123456", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new RemoteBuildVersionDetails
        {
            BuildNumber = 123456,
            FromShaId = "a1b2c3d4e5f6",
            Project = "DigitalServices/CustomerPortal",
            SourceBranch = "feature/123456-customer-portal",
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["VersionControlLog"] = JsonSerializer.SerializeToElement(new[]
                {
                    new
                    {
                        commitId = "a1b2c3d4e5f6",
                        comment = "Customer portal build for UAT"
                    }
                })
            }
        };
    }
}
