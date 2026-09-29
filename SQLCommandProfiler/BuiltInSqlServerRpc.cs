namespace SQLCommandProfiler;

internal static class BuiltInSqlServerRpc
{
    private static readonly HashSet<string> ReportExcludedShortNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "sp_reset_connection",
        "sp_executesql",
    };

    private static readonly HashSet<string> CaptureExcludedShortNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "sp_reset_connection",
    };

    public static bool IsExcludedFromCapture(string objectName)
    {
        return IsListed(objectName, CaptureExcludedShortNames);
    }

    public static bool IsExcludedFromTraceReport(string objectName)
    {
        return IsListed(objectName, ReportExcludedShortNames);
    }

    private static bool IsListed(string objectName, HashSet<string> shortNames)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        return shortNames.Contains(GetShortName(objectName));
    }

    private static string GetShortName(string objectName)
    {
        var normalized = objectName.Trim().Trim('[', ']');
        var lastDot = normalized.LastIndexOf('.');
        return lastDot >= 0 ? normalized[(lastDot + 1)..] : normalized;
    }
}
