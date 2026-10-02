using System.Globalization;
using System.Text.RegularExpressions;

namespace SQLCommandProfiler;

public enum ProfilerEventFilterEffect
{
    Exclude,
    Include,
}

public enum ProfilerEventFilterField
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
    ClientProcessId,
    SqlText,
}

public sealed record ProfilerEventFilterDefinition(
    ProfilerEventFilterEffect Effect,
    ProfilerEventFilterField Field,
    string Pattern);

public static class ProfilerEventFilterFieldValues
{
    public static string GetValue(in ExtendedEventInfo eventInfo, ProfilerEventFilterField field)
    {
        return field switch
        {
            ProfilerEventFilterField.DatabaseName => eventInfo.DatabaseName,
            ProfilerEventFilterField.ApplicationName => eventInfo.ApplicationName,
            ProfilerEventFilterField.UserName => eventInfo.UserName,
            ProfilerEventFilterField.HostName => eventInfo.HostName,
            ProfilerEventFilterField.QueryHash => eventInfo.QueryHash.ToString("X16", CultureInfo.InvariantCulture),
            ProfilerEventFilterField.QueryPlanHash => eventInfo.QueryPlanHash.ToString("X16", CultureInfo.InvariantCulture),
            ProfilerEventFilterField.EventName => eventInfo.EventName,
            ProfilerEventFilterField.ObjectName => eventInfo.ObjectName,
            ProfilerEventFilterField.SessionId => eventInfo.SessionId.ToString(CultureInfo.InvariantCulture),
            ProfilerEventFilterField.ClientProcessId => eventInfo.ClientProcessId.ToString(CultureInfo.InvariantCulture),
            ProfilerEventFilterField.SqlText => eventInfo.SqlText,
            _ => string.Empty,
        };
    }

    public static string SuggestPattern(in ExtendedEventInfo eventInfo, ProfilerEventFilterField field)
    {
        var value = GetValue(in eventInfo, field);
        if (string.IsNullOrEmpty(value))
        {
            return "^$";
        }

        return Regex.Escape(value);
    }
}
