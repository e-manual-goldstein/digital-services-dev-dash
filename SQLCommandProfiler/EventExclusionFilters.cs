using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

internal sealed class EventExclusionFilters
{
    private readonly Regex[] _excludeDatabaseNamePatterns;

    public EventExclusionFilters(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var patternsSection = configuration.GetSection("Profiler:EventFilters:ExcludeDatabaseNamePatterns");
        var patternStrings = new List<string>();
        foreach (var item in patternsSection.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(item.Value))
            {
                patternStrings.Add(item.Value);
            }
        }

        _excludeDatabaseNamePatterns = CompilePatterns(patternStrings);
    }

    public bool ShouldExclude(in ExtendedEventInfo eventInfo)
    {
        if (_excludeDatabaseNamePatterns.Length == 0)
        {
            return false;
        }

        return IsDatabaseNameExcluded(eventInfo.DatabaseName);
    }

    public bool IsDatabaseNameExcluded(string databaseName)
    {
        if (_excludeDatabaseNamePatterns.Length == 0)
        {
            return false;
        }

        foreach (var pattern in _excludeDatabaseNamePatterns)
        {
            if (pattern.IsMatch(databaseName))
            {
                return true;
            }
        }

        return false;
    }

    private static Regex[] CompilePatterns(IReadOnlyList<string> patternStrings)
    {
        if (patternStrings.Count == 0)
        {
            return [];
        }

        var compiled = new Regex[patternStrings.Count];
        for (var i = 0; i < patternStrings.Count; i++)
        {
            var pattern = patternStrings[i];
            try
            {
                compiled[i] = new Regex(
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    TimeSpan.FromSeconds(1));
            }
            catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
            {
                throw new InvalidOperationException(
                    $"Profiler:EventFilters:ExcludeDatabaseNamePatterns[{i}] is not a valid regular expression: \"{pattern}\".",
                    ex);
            }
        }

        return compiled;
    }
}
