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

    [TestMethod]
    public void FormatAdditionalPropertiesJson_ReturnsPrettyPrintedJsonOrNull()
    {
        var empty = new RemoteEnvironmentDetails();
        Assert.IsNull(empty.FormatAdditionalPropertiesJson());

        var details = JsonSerializer.Deserialize<RemoteEnvironmentDetails>(SampleJson);
        Assert.IsNotNull(details);

        var formatted = details!.FormatAdditionalPropertiesJson();
        Assert.IsNotNull(formatted);
        StringAssert.Contains(formatted, "\"SqlServerInstance\":");
        StringAssert.Contains(formatted, "\"BuildNumber\": \"123456\"");
        StringAssert.Contains(formatted, Environment.NewLine);
    }

    [TestMethod]
    public void Deserialize_MapsServersArrayAndExcludesFromAdditionalProperties()
    {
        const string json =
            """
            {
              "Id": 1,
              "Code": "UAT-01",
              "Name": "UAT-01",
              "EnvironmentType": "UAT",
              "Servers": [
                {
                  "ComponentName": "SQL Server",
                  "name": "UAT-01-SQL",
                  "ServerType": "Database",
                  "ComponentDescription": "Primary database server",
                  "ComponentIdenifier": "sql-01",
                  "ComponentResourceNameResolved": "UAT-01\\SQL2019"
                }
              ]
            }
            """;

        var details = JsonSerializer.Deserialize<RemoteEnvironmentDetails>(json);

        Assert.IsNotNull(details);
        Assert.HasCount(1, details!.Servers);
        Assert.AreEqual("SQL Server", details.Servers[0].ComponentName);
        Assert.AreEqual("UAT-01-SQL", details.Servers[0].Name);
        Assert.AreEqual("Database", details.Servers[0].ServerType);
        Assert.AreEqual("sql-01", details.Servers[0].ComponentIdenifier);
        Assert.AreEqual(@"UAT-01\SQL2019", details.Servers[0].ComponentResourceNameResolved);
        Assert.IsTrue(details.AdditionalProperties is null || details.AdditionalProperties.Count == 0);
    }

    [TestMethod]
    public void Deserialize_MapsWindowsServicesArray()
    {
        const string json =
            """
            {
              "Id": 1,
              "Code": "UAT-01",
              "Name": "UAT-01",
              "EnvironmentType": "UAT",
              "WindowsServices": [
                {
                  "MachineName": "UAT-01-APP",
                  "DisplayName": "Digital Services Worker",
                  "BinaryPathName": "C:\\Services\\DigitalServices.Worker.exe"
                }
              ]
            }
            """;

        var details = JsonSerializer.Deserialize<RemoteEnvironmentDetails>(json);

        Assert.IsNotNull(details);
        Assert.HasCount(1, details!.WindowsServices);
        Assert.AreEqual("UAT-01-APP", details.WindowsServices[0].MachineName);
        Assert.AreEqual("Digital Services Worker", details.WindowsServices[0].DisplayName);
        Assert.AreEqual(@"C:\Services\DigitalServices.Worker.exe", details.WindowsServices[0].BinaryPathName);
        Assert.AreEqual("Digital Services Worker", details.WindowsServices[0].ResolveDeployableApplicationName());
    }

    [TestMethod]
    public void Deserialize_MapsEnvironmentUrlsArray()
    {
        const string json =
            """
            {
              "Id": 1,
              "Code": "UAT-01",
              "Name": "UAT-01",
              "EnvironmentType": "UAT",
              "EnvironmentUrls": [
                {
                  "ApplicationName": "Customer Portal",
                  "Url": "https://uat-01.example.com/portal"
                }
              ]
            }
            """;

        var details = JsonSerializer.Deserialize<RemoteEnvironmentDetails>(json);

        Assert.IsNotNull(details);
        Assert.HasCount(1, details!.EnvironmentUrls);
        Assert.AreEqual("Customer Portal", details.EnvironmentUrls[0].ApplicationName);
        Assert.AreEqual("https://uat-01.example.com/portal", details.EnvironmentUrls[0].Url);
    }

    [TestMethod]
    public void Deserialize_MapsWebSitesArray()
    {
        const string json =
            """
            {
              "Id": 1,
              "Code": "UAT-01",
              "Name": "UAT-01",
              "EnvironmentType": "UAT",
              "WebSites": [
                {
                  "Name": "Default Web Site",
                  "MachineName": "UAT-01-APP",
                  "WebApplications": [
                    {
                      "ApplicationPoolName": "CustomerPortalAppPool",
                      "Path": "/portal",
                      "PhysicalPath": "C:\\\\inetpub\\\\wwwroot\\\\CustomerPortal"
                    }
                  ]
                }
              ]
            }
            """;

        var details = JsonSerializer.Deserialize<RemoteEnvironmentDetails>(json);

        Assert.IsNotNull(details);
        Assert.HasCount(1, details!.WebSites);
        Assert.AreEqual("Default Web Site", details.WebSites[0].Name);
        Assert.AreEqual("UAT-01-APP", details.WebSites[0].MachineName);
        Assert.AreEqual("Default Web Site - UAT-01-APP", details.WebSites[0].FormatSectionTitle());
        Assert.HasCount(1, details.WebSites[0].WebApplications);
        Assert.AreEqual("CustomerPortalAppPool", details.WebSites[0].WebApplications[0].ApplicationPoolName);
        Assert.AreEqual("/portal", details.WebSites[0].WebApplications[0].Path);
        Assert.AreEqual(@"C:\\inetpub\\wwwroot\\CustomerPortal", details.WebSites[0].WebApplications[0].PhysicalPath);
        Assert.AreEqual("portal", details.WebSites[0].WebApplications[0].ResolveDeployableApplicationName());
    }

    private sealed class RemoteEnvironmentDetailsWithSqlServer : RemoteEnvironmentDetails
    {
        public string? SqlServerInstance { get; set; }
    }
}
