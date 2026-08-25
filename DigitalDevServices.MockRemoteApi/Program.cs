using DigitalDevServices.MockRemoteApi;
using DigitalDevServices.Model;
using DigitalDevServices.Model.Environments;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/environments", () => Results.Ok(new RemoteApiResponse<RemoteEnvironmentDetails[]>
{
    Result = SampleEnvironments.All.ToArray()
}));

app.MapPost("/api/environments", (GetEnvironmentRequest request) =>
{
    var environment = SampleEnvironments.All.SingleOrDefault(item =>
        item.Code.Equals(request.EnvironmentCode, StringComparison.OrdinalIgnoreCase));

    return environment is null
        ? Results.NotFound()
        : Results.Ok(new RemoteApiResponse<RemoteEnvironmentDetails> { Result = environment });
});

app.MapPost("/api/environments/deployment-details", (GetEnvironmentRequest request) =>
{
    var deploymentDetails = SampleDeploymentDetails.ForEnvironmentCode(request.EnvironmentCode);

    return deploymentDetails is null
        ? Results.NotFound()
        : Results.Ok(new RemoteApiResponse<RemoteEnvironmentDeploymentDetails> { Result = deploymentDetails });
});

app.MapPost("/api/builds/version-details", (GetBuildVersionDetailsRequest request) =>
{
    var versionDetails = SampleBuildVersionDetails.ForBuildNumber(request.BuildNumber);

    return versionDetails is null
        ? Results.NotFound()
        : Results.Ok(new RemoteApiResponse<RemoteBuildVersionDetails> { Result = versionDetails });
});

app.MapGet("/", () => Results.Ok(new
{
    service = "DigitalDevServices.MockRemoteApi",
    endpoints = new[]
    {
        "GET /api/environments",
        "POST /api/environments",
        "POST /api/environments/deployment-details",
        "POST /api/builds/version-details"
    }
}));

app.Run();

public partial class Program;
