using Microsoft.Extensions.Configuration;

namespace DigitalDevServices.Data;

public static class DevDashDatabasePaths
{
    public const string ConnectionStringName = "DevDashDatabase";

    public static string GetDefaultDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DigitalDevServices",
            "DevDash");

        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "devdash.db");
    }

    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var path = GetDefaultDatabasePath();
        return $"Data Source={path};Cache=Shared";
    }
}
