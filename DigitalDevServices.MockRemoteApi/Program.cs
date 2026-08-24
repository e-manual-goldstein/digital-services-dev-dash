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

app.MapGet("/", () => Results.Ok(new
{
    service = "DigitalDevServices.MockRemoteApi",
    endpoints = new[]
    {
        "GET /api/environments",
        "POST /api/environments"
    }
}));

app.Run();

public partial class Program;
