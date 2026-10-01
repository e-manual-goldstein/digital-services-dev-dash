using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SQLCommandProfiler;

public static class SqlTextPrettyPrinter
{
    public static string FormatOrOriginal(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            return sqlText;
        }

        var parser = new TSql160Parser(initialQuotedIdentifiers: false);
        using var reader = new StringReader(sqlText);
        var fragment = parser.Parse(reader, out IList<ParseError> parseErrors);
        if (parseErrors.Count > 0 || fragment is null)
        {
            return sqlText;
        }

        return TSqlScriptFragmentFormatter.ToSql(fragment);
    }
}
