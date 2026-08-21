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
        Assert.IsTrue(environments!.Any(e => e.Name == "Partial16" && e.SqlServerInstance == @"PARTIAL16\SQL2019"));
    }

    [TestMethod]
    public async Task GetEnvironmentById_ReturnsMatchOrNotFound()
    {
        await using var factory = new WebApplicationFactory<global::Program>();
        using var client = factory.CreateClient();

        var partial16 = await client.GetFromJsonAsync<RemoteEnvironmentDetails>("/api/environments/1");
        Assert.IsNotNull(partial16);
        Assert.AreEqual("Partial16", partial16!.Name);

        var missing = await client.GetAsync("/api/environments/999");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
    }
}
