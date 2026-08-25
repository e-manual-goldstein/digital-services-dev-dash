using System.Text.Json;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class RemoteBuildVersionDetailsTests
{
    [TestMethod]
    public void Deserialize_MapsKnownFieldsAndCapturesOverflowInAdditionalProperties()
    {
        const string json = """
            {
              "BuildNumber": 123456,
              "FromShaId": "a1b2c3d4e5f6",
              "Project": "DigitalServices/CustomerPortal",
              "SourceBranch": "feature/123456-customer-portal",
              "VersionControlLog": [
                {
                  "commitId": "a1b2c3d4e5f6",
                  "comment": "Customer portal build for UAT"
                }
              ]
            }
            """;

        var details = JsonSerializer.Deserialize<RemoteBuildVersionDetails>(json);

        Assert.IsNotNull(details);
        Assert.AreEqual(123456, details!.BuildNumber);
        Assert.AreEqual("a1b2c3d4e5f6", details.FromShaId);
        Assert.AreEqual("DigitalServices/CustomerPortal", details.Project);
        Assert.AreEqual("feature/123456-customer-portal", details.SourceBranch);
        Assert.IsTrue(details.HasAdditionalProperties);
        Assert.IsTrue(details.AdditionalProperties!.ContainsKey("VersionControlLog"));
        Assert.IsFalse(details.AdditionalProperties.ContainsKey("BuildNumber"));
    }

    [TestMethod]
    public void FormatAdditionalPropertiesJson_ReturnsPrettyPrintedJsonOrNull()
    {
        var empty = new RemoteBuildVersionDetails();
        Assert.IsNull(empty.FormatAdditionalPropertiesJson());

        const string json = """
            {
              "BuildNumber": 1,
              "VersionControlLog": []
            }
            """;
        var details = JsonSerializer.Deserialize<RemoteBuildVersionDetails>(json);
        var formatted = details!.FormatAdditionalPropertiesJson();

        Assert.IsNotNull(formatted);
        StringAssert.Contains(formatted!, "\"VersionControlLog\"");
    }
}
