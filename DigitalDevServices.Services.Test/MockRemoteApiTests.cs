using System.Net.Http.Json;
using DigitalDevServices.Model;
using DigitalDevServices.Model.Environments;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class MockRemoteApiTests
{
    [TestMethod]
    public async Task GetEnvironments_ReturnsSampleEnvironments()
    {
        await using var factory = new WebApplicationFactory<global::Program>();
        using var client = factory.CreateClient();

        var wrapped = await client.GetFromJsonAsync<RemoteApiResponse<RemoteEnvironmentDetails[]>>("/api/environments");

        Assert.IsNotNull(wrapped);
        var environments = wrapped!.Result;
        Assert.IsNotNull(environments);
        Assert.HasCount(4, environments);
        Assert.IsTrue(environments.Any(environment =>
            environment.Name == "UAT-01"
            && environment.Code == "UAT-01"
            && environment.EnvironmentType == "UAT"
            && environment.Id == 1));
        var uat01 = environments.Single(environment => environment.Id == 1);
        Assert.IsTrue(uat01.TryGetAdditionalString("SqlServerInstance", out var sqlServerInstance));
        Assert.AreEqual(@"UAT-01\SQL2019", sqlServerInstance);
        Assert.IsFalse(uat01.TryGetAdditionalString("BuildNumber", out _));
        Assert.HasCount(2, uat01.Servers);
        Assert.AreEqual("UAT-01-SQL", uat01.Servers[0].Name);
        Assert.HasCount(2, uat01.WindowsServices);
        Assert.AreEqual("Digital Services Worker", uat01.WindowsServices[0].DisplayName);
        Assert.HasCount(2, uat01.EnvironmentUrls);
        Assert.AreEqual("Customer Portal", uat01.EnvironmentUrls[0].ApplicationName);
        Assert.HasCount(1, uat01.WebSites);
        Assert.AreEqual("Default Web Site", uat01.WebSites[0].Name);
        Assert.AreEqual("UAT-01-APP", uat01.WebSites[0].MachineName);
        Assert.HasCount(2, uat01.WebSites[0].WebApplications);
        Assert.AreEqual("/portal", uat01.WebSites[0].WebApplications[0].Path);
    }

    [TestMethod]
    public async Task GetEnvironmentByCode_ReturnsMatchOrNotFound()
    {
        await using var factory = new WebApplicationFactory<global::Program>();
        using var client = factory.CreateClient();

        var uat01 = await client.PostAsJsonAsync("/api/environments", new GetEnvironmentRequest
        {
            EnvironmentCode = "UAT-01"
        });
        Assert.IsTrue(uat01.IsSuccessStatusCode);
        var wrapped = await uat01.Content.ReadFromJsonAsync<RemoteApiResponse<RemoteEnvironmentDetails>>();
        Assert.IsNotNull(wrapped);
        var details = wrapped!.Result;
        Assert.IsNotNull(details);
        Assert.AreEqual("UAT-01", details!.Name);
        Assert.AreEqual("UAT", details.EnvironmentType);
        Assert.AreEqual(1, details.Id);
        Assert.IsTrue(details.TryGetAdditionalString("SqlServerInstance", out var sqlServerInstance));
        Assert.AreEqual(@"UAT-01\SQL2019", sqlServerInstance);
        Assert.HasCount(2, details.Servers);
        Assert.AreEqual("Database", details.Servers[0].ServerType);
        Assert.HasCount(2, details.WindowsServices);
        Assert.AreEqual(@"C:\Services\DigitalServices.Worker.exe", details.WindowsServices[0].BinaryPathName);
        Assert.HasCount(2, details.EnvironmentUrls);
        Assert.AreEqual("https://uat-01.example.com/api", details.EnvironmentUrls[1].Url);
        Assert.HasCount(1, details.WebSites);
        Assert.AreEqual(@"C:\inetpub\wwwroot\AdminApi", details.WebSites[0].WebApplications[1].PhysicalPath);

        var missing = await client.PostAsJsonAsync("/api/environments", new GetEnvironmentRequest
        {
            EnvironmentCode = "MISSING"
        });
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
    }

    [TestMethod]
    public async Task GetDeploymentDetailsForEnvironment_ReturnsMatchOrNotFound()
    {
        await using var factory = new WebApplicationFactory<global::Program>();
        using var client = factory.CreateClient();

        var uat01 = await client.PostAsJsonAsync("/api/environments/deployment-details", new GetEnvironmentRequest
        {
            EnvironmentCode = "UAT-01"
        });
        Assert.IsTrue(uat01.IsSuccessStatusCode);
        var wrapped = await uat01.Content.ReadFromJsonAsync<RemoteApiResponse<RemoteEnvironmentDeploymentDetails>>();
        Assert.IsNotNull(wrapped);
        var details = wrapped!.Result;
        Assert.IsNotNull(details);
        Assert.HasCount(1, details!.BuildsSuccessful);
        Assert.AreEqual(123456, details.BuildsSuccessful[0].BuildNumber);
        Assert.AreEqual("Customer Portal", details.BuildsSuccessful[0].Name);
        Assert.AreEqual("feature/123456-customer-portal", details.GetPrimaryWipBranch());

        var missing = await client.PostAsJsonAsync("/api/environments/deployment-details", new GetEnvironmentRequest
        {
            EnvironmentCode = "MISSING"
        });
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
    }
}
