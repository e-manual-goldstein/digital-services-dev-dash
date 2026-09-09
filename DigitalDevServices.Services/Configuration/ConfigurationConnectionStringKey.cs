namespace DigitalDevServices.Services.Configuration;

public static class ConfigurationConnectionStringKey
{
    public const string Prefix = "ConnectionStrings:";

    public static bool IsConnectionStringKey(string key) =>
        key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
}
