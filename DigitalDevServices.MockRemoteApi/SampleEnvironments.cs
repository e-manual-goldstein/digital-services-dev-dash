using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.MockRemoteApi;

internal static class SampleEnvironments
{
    public static IReadOnlyList<RemoteEnvironmentDetails> All { get; } =
    [
        new RemoteEnvironmentDetails
        {
            RemoteId = 1,
            Name = "UAT-01",
            SqlServerInstance = @"UAT-01\SQL2019",
            BuildNumber = "123456",
            WipBranch = "feature/123456-customer-portal"
        },
        new RemoteEnvironmentDetails
        {
            RemoteId = 2,
            Name = "Integration",
            SqlServerInstance = @"INT-SQL01\DEV",
            BuildNumber = "118902",
            WipBranch = "develop"
        },
        new RemoteEnvironmentDetails
        {
            RemoteId = 3,
            Name = "UAT",
            SqlServerInstance = @"UAT-SQL01\STD",
            BuildNumber = "120001",
            WipBranch = "release/12.0"
        },
        new RemoteEnvironmentDetails
        {
            RemoteId = 4,
            Name = "Production",
            SqlServerInstance = @"PROD-SQL01\STD",
            BuildNumber = "119500",
            WipBranch = "release/11.9"
        }
    ];
}
