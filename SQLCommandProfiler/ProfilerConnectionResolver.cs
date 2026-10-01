using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

public static class ProfilerConnectionResolver
{
    public static string Resolve(IConfiguration configuration, string? sqlServerInstance)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        if (string.IsNullOrWhiteSpace(sqlServerInstance))
        {
            return connectionString;
        }

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            DataSource = sqlServerInstance.Trim(),
        };
        return builder.ConnectionString;
    }
}
