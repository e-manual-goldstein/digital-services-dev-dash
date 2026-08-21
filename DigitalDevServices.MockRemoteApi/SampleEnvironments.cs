using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.MockRemoteApi;

internal static class SampleEnvironments
{
    public static IReadOnlyList<RemoteEnvironmentDetails> All { get; } =
    [
        new RemoteEnvironmentDetails
        {
            RemoteId = 1,
            Name = "Partial16",
            SqlServerInstance = @"PARTIAL16\SQL2019"
        },
        new RemoteEnvironmentDetails
        {
            RemoteId = 2,
            Name = "Integration",
            SqlServerInstance = @"INT-SQL01\DEV"
        },
        new RemoteEnvironmentDetails
        {
            RemoteId = 3,
            Name = "UAT",
            SqlServerInstance = @"UAT-SQL01\STD"
        },
        new RemoteEnvironmentDetails
        {
            RemoteId = 4,
            Name = "Production",
            SqlServerInstance = @"PROD-SQL01\STD"
        }
    ];
}
