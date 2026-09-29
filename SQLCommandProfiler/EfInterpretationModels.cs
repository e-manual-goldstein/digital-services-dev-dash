namespace SQLCommandProfiler;

public enum EfSqlStatementKind
{
    Other,
    Select,
    Insert,
    Update,
    Delete,
}

public sealed class EfTableReference
{
    public string? Schema { get; init; }

    public required string Name { get; init; }
}

public sealed class EfInsertDetails
{
    public required EfTableReference Table { get; init; }

    public IReadOnlyList<string> Columns { get; init; } = [];

    public IReadOnlyList<string> ValueExpressions { get; init; } = [];
}

public sealed class EfUpdateAssignment
{
    public required string Column { get; init; }

    public required string NewValueExpression { get; init; }
}

public sealed class EfUpdateDetails
{
    public required EfTableReference Table { get; init; }

    public IReadOnlyList<EfUpdateAssignment> Assignments { get; init; } = [];

    public string? WhereSql { get; init; }
}

public sealed class EfSqlStatementInterpretation
{
    public EfSqlStatementKind Kind { get; init; }

    public IReadOnlyList<EfTableReference> TablesRead { get; init; } = [];

    public EfInsertDetails? Insert { get; init; }

    public EfUpdateDetails? Update { get; init; }

    public EfTableReference? DeleteTarget { get; init; }
}
