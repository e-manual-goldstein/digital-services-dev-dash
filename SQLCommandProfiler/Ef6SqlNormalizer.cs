using System.Text.RegularExpressions;

namespace SQLCommandProfiler;

internal static partial class Ef6SqlNormalizer
{
    private static readonly Regex StatementStartPattern = StatementStartRegex();

    /// <summary>
    /// EF6 RPC batches often prefix the statement with parameter declarations, e.g.
    /// (@0 int,@1 datetime)UPDATE [dbo].[Table] ...
    /// (@0 int)SELECT ... AS [Extent1] ...
    /// </summary>
    public static string StripParameterPreamble(string sqlText)
    {
        var match = StatementStartPattern.Match(sqlText);
        return match.Success ? sqlText[match.Index..] : sqlText;
    }

    [GeneratedRegex(@"\b(?:INSERT|UPDATE|DELETE|SELECT)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StatementStartRegex();
}
