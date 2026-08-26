namespace DigitalDevServices.Model.Environments;

public sealed class DeployableApplicationRegistrationPrefill
{
    public required string Name { get; init; }

    public bool IsWebApp { get; init; } = true;

    public string? PathToLogFiles { get; init; }

    public string? PathToPhysicalPath { get; init; }
}
