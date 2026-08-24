using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.MockRemoteApi;

internal static class SampleEnvironments
{
    public static IReadOnlyList<RemoteEnvironmentDetails> All { get; } =
    [
        new RemoteEnvironmentDetails
        {
            Id = 1,
            Code = "UAT-01",
            Name = "UAT-01",
            EnvironmentType = "UAT"
        },
        new RemoteEnvironmentDetails
        {
            Id = 2,
            Code = "INT",
            Name = "Integration",
            EnvironmentType = "Integration"
        },
        new RemoteEnvironmentDetails
        {
            Id = 3,
            Code = "UAT",
            Name = "UAT",
            EnvironmentType = "UAT"
        },
        new RemoteEnvironmentDetails
        {
            Id = 4,
            Code = "PROD",
            Name = "Production",
            EnvironmentType = "Production"
        }
    ];
}
