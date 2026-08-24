using System.Text.Json;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class RemoteEnvironmentDetailsTests
{
    private const string SampleJson =
        """
        {
          "Id": 1,
          "Code": "UAT-01",
          "Name": "UAT-01",
          "EnvironmentType": "UAT",
          "SqlServerInstance": "UAT-01\\SQL2019",
          "BuildNumber": "123456",
          "WipBranch": "feature/123456-customer-portal"
        }
        """;

    [TestMethod]
    public void Deserialize_MapsKnownFieldsAndCapturesOverflowInAdditionalProperties()
    {
        var details = JsonSerializer.Deserialize<RemoteEnvironmentDetails>(SampleJson);

        Assert.IsNotNull(details);
        Assert.AreEqual(1, details!.Id);
        Assert.AreEqual("UAT-01", details.Code);
        Assert.AreEqual("UAT-01", details.Name);
        Assert.AreEqual("UAT", details.EnvironmentType);

        Assert.IsNotNull(details.AdditionalProperties);
        Assert.HasCount(3, details.AdditionalProperties!);
        Assert.IsTrue(details.AdditionalProperties!.ContainsKey("SqlServerInstance"));
        Assert.IsTrue(details.AdditionalProperties.ContainsKey("BuildNumber"));
        Assert.IsTrue(details.AdditionalProperties.ContainsKey("WipBranch"));
        Assert.IsFalse(details.AdditionalProperties.ContainsKey("Code"));

        Assert.IsTrue(details.TryGetAdditionalString("SqlServerInstance", out var sqlServerInstance));
        Assert.AreEqual(@"UAT-01\SQL2019", sqlServerInstance);
        Assert.IsTrue(details.TryGetAdditionalString("BuildNumber", out var buildNumber));
        Assert.AreEqual("123456", buildNumber);
    }

    [TestMethod]
    public void Deserialize_BindsPromotedPropertyInsteadOfAdditionalProperties()
    {
        var details = JsonSerializer.Deserialize<RemoteEnvironmentDetailsWithSqlServer>(SampleJson);

        Assert.IsNotNull(details);
        Assert.AreEqual(@"UAT-01\SQL2019", details!.SqlServerInstance);
        Assert.IsTrue(
            details.AdditionalProperties is null
            || !details.AdditionalProperties.ContainsKey("SqlServerInstance"));
        Assert.IsTrue(details.AdditionalProperties?.ContainsKey("BuildNumber") ?? false);
    }

    private sealed class RemoteEnvironmentDetailsWithSqlServer : RemoteEnvironmentDetails
    {
        public string? SqlServerInstance { get; set; }
    }
}
