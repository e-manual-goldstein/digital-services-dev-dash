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
        Assert.IsTrue(uat01.TryGetAdditionalString("BuildNumber", out var buildNumber));
        Assert.AreEqual("123456", buildNumber);
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

        var missing = await client.PostAsJsonAsync("/api/environments", new GetEnvironmentRequest
        {
            EnvironmentCode = "MISSING"
        });
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
    }
}
