using System.Net.Http.Json;
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

        var environments = await client.GetFromJsonAsync<List<RemoteEnvironmentDetails>>("/api/environments");

        Assert.IsNotNull(environments);
        Assert.HasCount(4, environments!);
        Assert.IsTrue(environments!.Any(e => e.Name == "UAT-01" && e.SqlServerInstance == @"UAT-01\SQL2019"));
        Assert.IsTrue(environments.Any(e => e.Name == "UAT-01" && e.BuildNumber == "123456" && e.WipBranch == "feature/123456-customer-portal"));
    }

    [TestMethod]
    public async Task GetEnvironmentById_ReturnsMatchOrNotFound()
    {
        await using var factory = new WebApplicationFactory<global::Program>();
        using var client = factory.CreateClient();

        var uat01 = await client.GetFromJsonAsync<RemoteEnvironmentDetails>("/api/environments/1");
        Assert.IsNotNull(uat01);
        Assert.AreEqual("UAT-01", uat01!.Name);
        Assert.AreEqual("123456", uat01.BuildNumber);
        Assert.AreEqual("feature/123456-customer-portal", uat01.WipBranch);

        var missing = await client.GetAsync("/api/environments/999");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
    }
}
