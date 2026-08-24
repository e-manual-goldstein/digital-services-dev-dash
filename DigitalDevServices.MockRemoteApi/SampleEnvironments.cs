using System.Text.Json;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.MockRemoteApi;

internal static class SampleEnvironments
{
    public static IReadOnlyList<RemoteEnvironmentDetails> All { get; } =
    [
        WithAdditionalProperties(
            new RemoteEnvironmentDetails
            {
                Id = 1,
                Code = "UAT-01",
                Name = "UAT-01",
                EnvironmentType = "UAT",
                Servers =
                [
                    new EnvironmentServer
                    {
                        ComponentName = "SQL Server",
                        Name = "UAT-01-SQL",
                        ServerType = "Database",
                        ComponentDescription = "Primary database server",
                        ComponentIdenifier = "sql-01",
                        ComponentResourceNameResolved = @"UAT-01\SQL2019"
                    },
                    new EnvironmentServer
                    {
                        ComponentName = "Application Server",
                        Name = "UAT-01-APP",
                        ServerType = "Web",
                        ComponentDescription = "IIS host",
                        ComponentIdenifier = "app-01",
                        ComponentResourceNameResolved = "UAT-01-APP.example.com"
                    }
                ],
                WindowsServices =
                [
                    new EnvironmentWindowsService
                    {
                        MachineName = "UAT-01-APP",
                        DisplayName = "Digital Services Worker",
                        BinaryPathName = @"C:\Services\DigitalServices.Worker.exe"
                    },
                    new EnvironmentWindowsService
                    {
                        MachineName = "UAT-01-APP",
                        DisplayName = "Message Queue Listener",
                        BinaryPathName = @"C:\Services\MessageQueue.Listener.exe"
                    }
                ]
            },
            ("SqlServerInstance", @"UAT-01\SQL2019"),
            ("BuildNumber", "123456"),
            ("WipBranch", "feature/123456-customer-portal")),
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

    private static RemoteEnvironmentDetails WithAdditionalProperties(
        RemoteEnvironmentDetails environment,
        params (string Name, object Value)[] additionalProperties)
    {
        environment.AdditionalProperties = additionalProperties.ToDictionary(
            property => property.Name,
            property => JsonSerializer.SerializeToElement(property.Value));

        return environment;
    }
}
