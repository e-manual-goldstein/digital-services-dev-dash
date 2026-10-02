using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

internal sealed record EventFilterRule(ProfilerEventFilterEffect Effect, ProfilerEventFilterField Field, Regex Pattern);

internal sealed class EventFilterEngine
{
    private readonly object _sync = new();
    private List<EventFilterRule> _rules;
    private bool _hasIncludeRules;

    public EventFilterEngine(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _rules = LoadRules(configuration.GetSection("Profiler:EventFilters:Rules")).ToList();
        _hasIncludeRules = _rules.Any(rule => rule.Effect == ProfilerEventFilterEffect.Include);
    }

    public bool TryAddRule(ProfilerEventFilterDefinition definition, out string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Pattern))
        {
            errorMessage = "Pattern is required.";
            return false;
        }

        if (!TryCompilePattern(definition.Pattern, out var pattern, out errorMessage))
        {
            return false;
        }

        lock (_sync)
        {
            _rules.Add(new EventFilterRule(definition.Effect, definition.Field, pattern));
            _hasIncludeRules = _rules.Any(rule => rule.Effect == ProfilerEventFilterEffect.Include);
        }

        errorMessage = null;
        return true;
    }

    public bool PassesFilters(in ExtendedEventInfo eventInfo)
    {
        EventFilterRule[] rules;
        var hasIncludeRules = false;

        lock (_sync)
        {
            rules = _rules.ToArray();
            hasIncludeRules = _hasIncludeRules;
        }

        foreach (var rule in rules)
        {
            if (rule.Effect != ProfilerEventFilterEffect.Exclude)
            {
                continue;
            }

            if (RuleMatches(rule, eventInfo))
            {
                return false;
            }
        }

        if (!hasIncludeRules)
        {
            return true;
        }

        foreach (var rule in rules)
        {
            if (rule.Effect != ProfilerEventFilterEffect.Include)
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
        var value = ProfilerEventFilterFieldValues.GetValue(in eventInfo, rule.Field);
        return rule.Pattern.IsMatch(value);
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

            if (!Enum.TryParse(effectText, ignoreCase: true, out ProfilerEventFilterEffect effect))
            {
                throw new InvalidOperationException(
                    $"Profiler:EventFilters:Rules:{index}:Effect \"{effectText}\" is invalid. Use Exclude or Include.");
            }

            if (!Enum.TryParse(fieldText, ignoreCase: true, out ProfilerEventFilterField field))
            {
                throw new InvalidOperationException(
                    $"Profiler:EventFilters:Rules:{index}:Field \"{fieldText}\" is invalid.");
            }

            if (!TryCompilePattern(patternText, out var pattern, out var errorMessage))
            {
                throw new InvalidOperationException(
                    $"Profiler:EventFilters:Rules:{index}:Pattern is not a valid regular expression: \"{patternText}\". {errorMessage}");
            }

            rules.Add(new EventFilterRule(effect, field, pattern));
            index++;
        }

        return rules.ToArray();
    }

    private static bool TryCompilePattern(string patternText, out Regex pattern, out string? errorMessage)
    {
        try
        {
            pattern = new Regex(
                patternText,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                TimeSpan.FromSeconds(1));
            errorMessage = null;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            pattern = null!;
            errorMessage = ex.Message;
            return false;
        }
    }

    private static bool IsRuleInactive(IConfigurationSection ruleSection)
    {
        return bool.TryParse(ruleSection["IsActive"], out var isActive) && !isActive;
    }
}
