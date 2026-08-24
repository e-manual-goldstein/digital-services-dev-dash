using DigitalDevServices.MockRemoteApi;
using DigitalDevServices.Model.Environments;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/environments", () => Results.Ok(SampleEnvironments.All));

app.MapPost("/api/environments", (GetEnvironmentRequest request) =>
{
    var environment = SampleEnvironments.All.SingleOrDefault(item =>
        item.Code.Equals(request.EnvironmentCode, StringComparison.OrdinalIgnoreCase));

    return environment is null ? Results.NotFound() : Results.Ok(environment);
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
