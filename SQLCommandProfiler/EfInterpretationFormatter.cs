namespace SQLCommandProfiler;

internal static class EfInterpretationFormatter
{
    public static IEnumerable<string> FormatSummaryLines(EfInterpretation interpretation)
    {
        yield return $"ef6 access={interpretation.Access}";

        foreach (var statement in interpretation.Statements)
        {
            foreach (var line in FormatStatement(statement))
            {
                yield return line;
            }
        }
    }

    private static IEnumerable<string> FormatStatement(EfSqlStatementInterpretation statement)
    {
        switch (statement.Kind)
        {
            case EfSqlStatementKind.Select:
                yield return $"  select tables: {FormatTableList(statement.TablesRead)}";
                break;
            case EfSqlStatementKind.Insert when statement.Insert is not null:
                yield return $"  insert {FormatTable(statement.Insert.Table)} columns=[{string.Join(", ", statement.Insert.Columns)}] values=[{string.Join(", ", statement.Insert.ValueExpressions)}]";
                break;
            case EfSqlStatementKind.Update when statement.Update is not null:
                var sets = string.Join(", ", statement.Update.Assignments.Select(assignment => $"{assignment.Column}={assignment.NewValueExpression}"));
                yield return $"  update {FormatTable(statement.Update.Table)} set {sets}";
                if (!string.IsNullOrWhiteSpace(statement.Update.WhereSql))
                {
                    yield return $"    where {statement.Update.WhereSql}";
                }

                break;
            case EfSqlStatementKind.Delete when statement.DeleteTarget is not null:
                yield return $"  delete {FormatTable(statement.DeleteTarget)}";
                break;
        }
    }

    private static string FormatTableList(IReadOnlyList<EfTableReference> tables)
    {
        if (tables.Count == 0)
        {
            return "(none)";
        }

        return string.Join(", ", tables.Select(FormatTable));
    }

    private static string FormatTable(EfTableReference table)
    {
        return string.IsNullOrEmpty(table.Schema)
            ? table.Name
            : $"{table.Schema}.{table.Name}";
    }
}
