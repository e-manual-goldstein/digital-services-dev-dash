namespace DigitalDevServices.Model;

/// <summary>
/// Standard envelope returned by external team Web APIs: a single <see cref="Result"/> payload.
/// </summary>
public sealed class RemoteApiResponse<T>
{
    public required T Result { get; set; }
}
