using System.Text.RegularExpressions;

namespace SQLCommandProfiler;

internal static partial class Ef6SqlDetector
{
    private static readonly Regex ExtentOrProjectAliasPattern = ExtentProjectAliasRegex();
    private static readonly Regex Ef6BracketedDmlPattern = Ef6BracketedDmlRegex();
    private static readonly Regex Ef6SelectPattern = Ef6SelectRegex();
    private static readonly Regex EfParameterPattern = EfParameterRegex();

    public static bool LooksLikeEf6(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            return false;
        }

        if (sqlText.Contains("[Extent", StringComparison.OrdinalIgnoreCase)
            || sqlText.Contains("[Project", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ExtentOrProjectAliasPattern.IsMatch(sqlText))
        {
            return true;
        }

        if (Ef6BracketedDmlPattern.IsMatch(sqlText)
            && EfParameterPattern.IsMatch(sqlText))
        {
            return true;
        }

        if (Ef6SelectPattern.IsMatch(sqlText) && EfParameterPattern.IsMatch(sqlText))
        {
            return true;
        }

        return false;
    }

    [GeneratedRegex(@"\bSELECT\b[\s\S]*\[[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Ef6SelectRegex();

    [GeneratedRegex(@"(?:INSERT|UPDATE|DELETE)\s+\[(?:[^\]]+\]\.)?\[[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Ef6BracketedDmlRegex();

    [GeneratedRegex(@"@\w+", RegexOptions.CultureInvariant)]
    private static partial Regex EfParameterRegex();

    [GeneratedRegex(@"AS\s+\[(?:Extent|Project)\d+\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExtentProjectAliasRegex();
}
