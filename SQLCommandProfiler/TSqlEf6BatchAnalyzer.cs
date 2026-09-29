using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SQLCommandProfiler;

internal sealed class TSqlEf6BatchAnalyzer
{
    private readonly TSql160Parser _parser = new(initialQuotedIdentifiers: false);

    public bool TryAnalyze(string sqlText, out EfInterpretation interpretation, out IList<ParseError> parseErrors)
    {
        interpretation = new EfInterpretation { IsLikelyEf6 = true };
        parseErrors = [];

        TSqlFragment fragment;
        using (var reader = new StringReader(sqlText))
        {
            fragment = _parser.Parse(reader, out parseErrors);
        }

        if (fragment is not TSqlScript script)
        {
            interpretation = new EfInterpretation
            {
                IsLikelyEf6 = true,
                Access = EfSqlAccess.Unknown,
            };
            return false;
        }

        var statements = new List<EfSqlStatementInterpretation>();
        foreach (var batch in script.Batches)
        {
            foreach (var statement in batch.Statements)
            {
                if (TryInterpretStatement(statement, out var interpreted))
                {
                    statements.Add(interpreted);
                }
            }
        }

        interpretation = new EfInterpretation
        {
            IsLikelyEf6 = true,
            Statements = statements,
            Access = ClassifyAccess(statements),
        };

        return true;
    }

    private static EfSqlAccess ClassifyAccess(IReadOnlyList<EfSqlStatementInterpretation> statements)
    {
        if (statements.Count == 0)
        {
            return EfSqlAccess.Unknown;
        }

        var hasWrite = statements.Any(statement =>
            statement.Kind is EfSqlStatementKind.Insert
                or EfSqlStatementKind.Update
                or EfSqlStatementKind.Delete);

        if (hasWrite)
        {
            return EfSqlAccess.ReadWrite;
        }

        var hasSelect = statements.Any(statement => statement.Kind == EfSqlStatementKind.Select);
        return hasSelect ? EfSqlAccess.ReadOnly : EfSqlAccess.Unknown;
    }

    private static bool TryInterpretStatement(TSqlStatement statement, out EfSqlStatementInterpretation interpretation)
    {
        switch (statement)
        {
            case SelectStatement select:
                interpretation = InterpretSelect(select);
                return true;
            case InsertStatement insert:
                interpretation = InterpretInsert(insert);
                return true;
            case UpdateStatement update:
                interpretation = InterpretUpdate(update);
                return true;
            case DeleteStatement delete:
                interpretation = InterpretDelete(delete);
                return true;
            case DeclareVariableStatement:
            case SetVariableStatement:
            case IfStatement:
            case BeginEndBlockStatement:
                interpretation = new EfSqlStatementInterpretation { Kind = EfSqlStatementKind.Other };
                return false;
            default:
                interpretation = new EfSqlStatementInterpretation { Kind = EfSqlStatementKind.Other };
                return false;
        }
    }

    private static EfSqlStatementInterpretation InterpretSelect(SelectStatement select)
    {
        var tables = new List<EfTableReference>();
        if (select.QueryExpression is QuerySpecification specification)
        {
            CollectTablesFromTableExpression(specification.FromClause?.TableReferences, tables);
        }
        else if (select.QueryExpression is BinaryQueryExpression binary)
        {
            CollectTablesFromQueryExpression(binary.FirstQueryExpression, tables);
            CollectTablesFromQueryExpression(binary.SecondQueryExpression, tables);
        }

        return new EfSqlStatementInterpretation
        {
            Kind = EfSqlStatementKind.Select,
            TablesRead = tables.DistinctBy(table => $"{table.Schema}.{table.Name}").ToArray(),
        };
    }

    private static void CollectTablesFromQueryExpression(QueryExpression expression, List<EfTableReference> tables)
    {
        if (expression is QuerySpecification specification)
        {
            CollectTablesFromTableExpression(specification.FromClause?.TableReferences, tables);
        }
        else if (expression is BinaryQueryExpression binary)
        {
            CollectTablesFromQueryExpression(binary.FirstQueryExpression, tables);
            CollectTablesFromQueryExpression(binary.SecondQueryExpression, tables);
        }
    }

    private static void CollectTablesFromTableExpression(IList<TableReference>? references, List<EfTableReference> tables)
    {
        if (references is null)
        {
            return;
        }

        foreach (var reference in references)
        {
            CollectTablesFromTableReference(reference, tables);
        }
    }

    private static void CollectTablesFromTableReference(TableReference reference, List<EfTableReference> tables)
    {
        switch (reference)
        {
            case NamedTableReference named:
                tables.Add(ToTableReference(named.SchemaObject));
                break;
            case QualifiedJoin join:
                CollectTablesFromTableReference(join.FirstTableReference, tables);
                CollectTablesFromTableReference(join.SecondTableReference, tables);
                break;
        }
    }

    private static EfSqlStatementInterpretation InterpretInsert(InsertStatement insert)
    {
        var spec = insert.InsertSpecification;
        var table = spec?.Target is NamedTableReference namedTarget
            ? ToTableReference(namedTarget)
            : new EfTableReference { Name = string.Empty };
        var columns = spec?.Columns?.Select(column => IdentifierName(column.MultiPartIdentifier)).ToArray() ?? [];
        var valueExpressions = ExtractInsertValueExpressions(spec?.InsertSource);

        return new EfSqlStatementInterpretation
        {
            Kind = EfSqlStatementKind.Insert,
            Insert = new EfInsertDetails
            {
                Table = table,
                Columns = columns,
                ValueExpressions = valueExpressions,
            },
        };
    }

    private static IReadOnlyList<string> ExtractInsertValueExpressions(InsertSource? source)
    {
        if (source is not ValuesInsertSource valuesSource)
        {
            return [];
        }

        var expressions = new List<string>();
        foreach (var row in valuesSource.RowValues)
        {
            foreach (var value in row.ColumnValues)
            {
                expressions.Add(TSqlScriptFragmentFormatter.ToSql(value));
            }
        }

        return expressions;
    }

    private static EfSqlStatementInterpretation InterpretUpdate(UpdateStatement update)
    {
        var spec = update.UpdateSpecification;
        var table = spec?.Target is NamedTableReference namedTarget
            ? ToTableReference(namedTarget)
            : new EfTableReference { Name = string.Empty };
        var assignments = spec?.SetClauses?
            .OfType<AssignmentSetClause>()
            .Select(clause => new EfUpdateAssignment
            {
                Column = IdentifierName(clause.Column.MultiPartIdentifier),
                NewValueExpression = TSqlScriptFragmentFormatter.ToSql(clause.NewValue),
            })
            .ToArray() ?? [];

        var whereSql = spec?.WhereClause is null
            ? null
            : TSqlScriptFragmentFormatter.ToSql(spec.WhereClause);

        return new EfSqlStatementInterpretation
        {
            Kind = EfSqlStatementKind.Update,
            Update = new EfUpdateDetails
            {
                Table = table,
                Assignments = assignments,
                WhereSql = whereSql,
            },
        };
    }

    private static EfSqlStatementInterpretation InterpretDelete(DeleteStatement delete)
    {
        var spec = delete.DeleteSpecification;
        var target = spec?.Target is NamedTableReference namedTarget
            ? ToTableReference(namedTarget)
            : new EfTableReference { Name = string.Empty };
        var tables = new List<EfTableReference> { target };
        CollectTablesFromTableExpression(spec?.FromClause?.TableReferences, tables);

        return new EfSqlStatementInterpretation
        {
            Kind = EfSqlStatementKind.Delete,
            DeleteTarget = target,
            TablesRead = tables.DistinctBy(table => $"{table.Schema}.{table.Name}").ToArray(),
        };
    }

    private static EfTableReference ToTableReference(NamedTableReference? reference)
    {
        return ToTableReference(reference?.SchemaObject);
    }

    private static EfTableReference ToTableReference(SchemaObjectName? schemaObject)
    {
        if (schemaObject is null || schemaObject.Identifiers.Count == 0)
        {
            return new EfTableReference { Name = string.Empty };
        }

        if (schemaObject.Identifiers.Count == 1)
        {
            return new EfTableReference { Name = schemaObject.Identifiers[0].Value };
        }

        return new EfTableReference
        {
            Schema = schemaObject.Identifiers[^2].Value,
            Name = schemaObject.Identifiers[^1].Value,
        };
    }

    private static string IdentifierName(MultiPartIdentifier identifier)
    {
        return identifier.Identifiers[^1].Value;
    }
}
