namespace DigitalDevServices.Model.Environments;

public class EnvironmentCacheOptions
{
    public const string SectionName = "EnvironmentCache";

    /// <summary>
    /// How long cached remote environment details remain valid before refetching the API.
    /// </summary>
    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromHours(24);
}
