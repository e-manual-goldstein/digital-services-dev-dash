namespace DigitalDevServices.Model.Environments;

/// <summary>
/// A tracked environment combined with cached remote API details.
/// </summary>
public record CachedEnvironment
{
    public required Guid LocalId { get; init; }

    public required int RemoteId { get; init; }

    public required bool IsFavourite { get; init; }

    public required RemoteEnvironmentDetails Details { get; init; }

    public required DateTimeOffset DateLastUpdated { get; init; }

    public required bool IsFromCache { get; init; }
}
