using System.Text.RegularExpressions;

namespace SQLCommandProfiler;

internal static partial class Ef6SqlDetector
{
    private static readonly Regex ExtentOrProjectAliasPattern = ExtentProjectAliasRegex();
    private static readonly Regex Ef6BracketedDmlPattern = Ef6BracketedDmlRegex();

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
            && sqlText.Contains("@p", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    [GeneratedRegex(@"(?:INSERT|UPDATE|DELETE)\s+\[(?:[^\]]+\]\.)?\[[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Ef6BracketedDmlRegex();

    [GeneratedRegex(@"AS\s+\[(?:Extent|Project)\d+\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExtentProjectAliasRegex();
}
