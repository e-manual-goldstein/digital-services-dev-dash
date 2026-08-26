namespace DigitalDevServices.Model.Environments;

public sealed class RemoteRegistrationPrefill
{
    public required Guid EnvironmentLocalId { get; init; }

    public required RemoteRegistrationSource Source { get; init; }

    public DeployableApplicationRegistrationPrefill? Application { get; init; }

    public required ApplicationInstanceRegistrationPrefill Instance { get; init; }

    public bool RequiresApplicationCreate => Application is not null;

    public bool IsUpdate => Instance.ExistingInstanceId is not null;

    public RemoteRegistrationPrefill ContinueAfterApplicationCreated(Guid deployableApplicationId) =>
        new()
        {
            EnvironmentLocalId = EnvironmentLocalId,
            Source = Source,
            Application = null,
            Instance = Instance.WithDeployableApplicationId(deployableApplicationId)
        };
}
