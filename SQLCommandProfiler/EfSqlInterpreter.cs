namespace SQLCommandProfiler;

/// <summary>
/// Parses captured SQL for EF6-style batches. EF Core support is planned separately.
/// </summary>
internal sealed class EfSqlInterpreter
{
    public EfInterpretation? Interpret(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            return null;
        }

        // EF6 detection and ScriptDom extraction will be implemented here.
        return null;
    }
}
