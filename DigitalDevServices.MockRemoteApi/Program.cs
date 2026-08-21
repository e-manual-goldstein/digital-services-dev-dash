using DigitalDevServices.MockRemoteApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/environments", () => Results.Ok(SampleEnvironments.All));

app.MapGet("/api/environments/{id:int}", (int id) =>
{
    var environment = SampleEnvironments.All.SingleOrDefault(e => e.RemoteId == id);
    return environment is null ? Results.NotFound() : Results.Ok(environment);
});

app.MapGet("/", () => Results.Ok(new
{
    service = "DigitalDevServices.MockRemoteApi",
    endpoints = new[]
    {
        "GET /api/environments",
        "GET /api/environments/{id}"
    }
}));

app.Run();

public partial class Program;
