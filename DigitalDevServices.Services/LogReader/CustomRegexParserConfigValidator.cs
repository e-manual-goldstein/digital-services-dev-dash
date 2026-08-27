using System.Text.RegularExpressions;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public static class CustomRegexParserConfigValidator
{
    public static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(1);

    public static Regex CompilePattern(string pattern)
    {
        try
        {
            return new Regex(
                pattern,
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                RegexMatchTimeout);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            throw new InvalidOperationException($"Custom regex pattern is invalid: {ex.Message}", ex);
        }
    }

    public static void Validate(CustomRegexParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var regex = CompilePattern(config.Pattern);
        var groupNames = regex.GetGroupNames();
        if (!groupNames.Contains("message", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Custom regex pattern must include a named capture group 'message'.");
        }
    }

    public static void ValidateJson(string? parserConfigJson)
    {
        var config = CustomRegexParserConfig.Parse(parserConfigJson);
        Validate(config);
    }
}
