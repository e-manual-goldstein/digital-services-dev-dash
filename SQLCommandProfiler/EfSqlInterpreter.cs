namespace SQLCommandProfiler;

/// <summary>
/// Parses captured SQL for EF6-style batches. EF Core support is planned separately.
/// </summary>
public sealed class EfSqlInterpreter
{
    private readonly TSqlEf6BatchAnalyzer _analyzer = new();

    public EfInterpretation? Interpret(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            return null;
        }

        if (!Ef6SqlDetector.LooksLikeEf6(sqlText))
        {
            return null;
        }

        var normalizedSql = Ef6SqlNormalizer.StripParameterPreamble(sqlText);

        if (!_analyzer.TryAnalyze(normalizedSql, out var interpretation, out _))
        {
            return new EfInterpretation
            {
                IsLikelyEf6 = true,
                Access = EfSqlAccess.Unknown,
            };
        }

        return interpretation;
    }
}
