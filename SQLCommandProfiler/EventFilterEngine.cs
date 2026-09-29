using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

internal enum EventFilterEffect
{
    Exclude,
    Include,
}

internal enum EventFilterField
{
    DatabaseName,
    ApplicationName,
    UserName,
    HostName,
    QueryHash,
    QueryPlanHash,
    EventName,
    ObjectName,
    SessionId,
    SqlText,
}

internal sealed record EventFilterRule(EventFilterEffect Effect, EventFilterField Field, Regex Pattern);

internal sealed class EventFilterEngine
{
    private readonly EventFilterRule[] _rules;
    private readonly bool _hasIncludeRules;

    public EventFilterEngine(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _rules = LoadRules(configuration.GetSection("Profiler:EventFilters:Rules"));
        _hasIncludeRules = _rules.Any(rule => rule.Effect == EventFilterEffect.Include);
    }

    public bool PassesFilters(in ExtendedEventInfo eventInfo)
    {
        foreach (var rule in _rules)
        {
            if (rule.Effect != EventFilterEffect.Exclude)
            {
                continue;
            }

            if (RuleMatches(rule, eventInfo))
            {
                return false;
            }
        }

        if (!_hasIncludeRules)
        {
            return true;
        }

        foreach (var rule in _rules)
        {
            if (rule.Effect != EventFilterEffect.Include)
            {
                continue;
            }

            if (RuleMatches(rule, eventInfo))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RuleMatches(EventFilterRule rule, in ExtendedEventInfo eventInfo)
    {
        var value = GetFieldValue(eventInfo, rule.Field);
        return rule.Pattern.IsMatch(value);
    }

    private static string GetFieldValue(in ExtendedEventInfo eventInfo, EventFilterField field)
    {
        return field switch
        {
            EventFilterField.DatabaseName => eventInfo.DatabaseName,
            EventFilterField.ApplicationName => eventInfo.ApplicationName,
            EventFilterField.UserName => eventInfo.UserName,
            EventFilterField.HostName => eventInfo.HostName,
            EventFilterField.QueryHash => eventInfo.QueryHash.ToString("X16", CultureInfo.InvariantCulture),
            EventFilterField.QueryPlanHash => eventInfo.QueryPlanHash.ToString("X16", CultureInfo.InvariantCulture),
            EventFilterField.EventName => eventInfo.EventName,
            EventFilterField.ObjectName => eventInfo.ObjectName,
            EventFilterField.SessionId => eventInfo.SessionId.ToString(CultureInfo.InvariantCulture),
            EventFilterField.SqlText => throw new InvalidOperationException(
                "SqlText filtering is not supported yet. Remove or change rules that use Field \"SqlText\"."),
            _ => string.Empty,
        };
    }

    private static EventFilterRule[] LoadRules(IConfigurationSection rulesSection)
    {
        var rules = new List<EventFilterRule>();
        var index = 0;

        foreach (var ruleSection in rulesSection.GetChildren())
        {
            if (IsRuleInactive(ruleSection))
            {
                index++;
                continue;
            }

            var effectText = ruleSection["Effect"]
                ?? throw new InvalidOperationException($"Profiler:EventFilters:Rules:{index}:Effect is required.");
            var fieldText = ruleSection["Field"]
                ?? throw new InvalidOperationException($"Profiler:EventFilters:Rules:{index}:Field is required.");
            var patternText = ruleSection["Pattern"]
                ?? throw new InvalidOperationException($"Profiler:EventFilters:Rules:{index}:Pattern is required.");

            if (!Enum.TryParse(effectText, ignoreCase: true, out EventFilterEffect effect))
            {
                throw new InvalidOperationException(
                    $"Profiler:EventFilters:Rules:{index}:Effect \"{effectText}\" is invalid. Use Exclude or Include.");
            }

            if (!Enum.TryParse(fieldText, ignoreCase: true, out EventFilterField field))
            {
                throw new InvalidOperationException(
                    $"Profiler:EventFilters:Rules:{index}:Field \"{fieldText}\" is invalid.");
            }

            if (field == EventFilterField.SqlText)
            {
                throw new InvalidOperationException(
                    "SqlText filtering is not supported yet. Remove or change rules that use Field \"SqlText\".");
            }

            Regex pattern;
            try
            {
                pattern = new Regex(
                    patternText,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    TimeSpan.FromSeconds(1));
            }
            catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
            {
                throw new InvalidOperationException(
                    $"Profiler:EventFilters:Rules:{index}:Pattern is not a valid regular expression: \"{patternText}\".",
                    ex);
            }

            rules.Add(new EventFilterRule(effect, field, pattern));
            index++;
        }

        return rules.ToArray();
    }

    private static bool IsRuleInactive(IConfigurationSection ruleSection)
    {
        return bool.TryParse(ruleSection["IsActive"], out var isActive) && !isActive;
    }
}
