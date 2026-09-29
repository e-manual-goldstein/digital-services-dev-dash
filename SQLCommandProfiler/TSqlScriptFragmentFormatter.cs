using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SQLCommandProfiler;

internal static class TSqlScriptFragmentFormatter
{
    public static string ToSql(TSqlFragment fragment)
    {
        using var writer = new StringWriter();
        var generator = new Sql160ScriptGenerator();
        generator.GenerateScript(fragment, writer);
        return writer.ToString().Trim();
    }
}
