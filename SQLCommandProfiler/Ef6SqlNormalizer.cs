using System.Text.RegularExpressions;

namespace SQLCommandProfiler;

internal static partial class Ef6SqlNormalizer
{
    private static readonly Regex DmlStartPattern = DmlStartRegex();

    /// <summary>
    /// EF6 RPC batches often prefix the statement with parameter declarations, e.g.
    /// (@0 int,@1 datetime)UPDATE [dbo].[Table] ...
    /// </summary>
    public static string StripParameterPreamble(string sqlText)
    {
        var match = DmlStartPattern.Match(sqlText);
        return match.Success ? sqlText[match.Index..] : sqlText;
    }

    [GeneratedRegex(@"(?:INSERT|UPDATE|DELETE)\s+\[", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DmlStartRegex();
}
