using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

internal class Program
{
    static void Main(string[] args)
    {
        var config = GetConfigSettings();

        try
        {
            using (var helper = new ProfileHelper(config))
            {
                helper.BeginTrace();

                Console.WriteLine("Trace running. Press any key to stop (you will be asked to confirm).");

                while (true)
                {
                    Console.ReadKey(intercept: true);
                    Console.WriteLine();
                    Console.Write("Do you want to end the trace? (y/n): ");

                    if (IsAffirmative(Console.ReadLine()))
                    {
                        break;
                    }

                    Console.WriteLine("Trace continues. Press any key when you want to stop again.");
                }

                helper.EndTrace();
                var reportPath = helper.CreateTraceReport();
                Console.WriteLine($"Trace report written to: {reportPath}");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            if (ex.InnerException is not null)
            {
                Console.Error.WriteLine(ex.InnerException);
            }

            Environment.ExitCode = 1;
        }
    }

    static IConfiguration GetConfigSettings()
    {
        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    static bool IsAffirmative(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        return response.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }
}
