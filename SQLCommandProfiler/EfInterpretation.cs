namespace SQLCommandProfiler;

public enum EfSqlAccess
{
    Unknown,
    ReadOnly,
    ReadWrite,
}

/// <summary>
/// Result of interpreting a captured SQL batch as Entity Framework (EF6) SQL.
/// </summary>
public sealed class EfInterpretation
{
    public required bool IsLikelyEf6 { get; init; }

    public EfSqlAccess Access { get; init; } = EfSqlAccess.Unknown;

    public IReadOnlyList<EfSqlStatementInterpretation> Statements { get; init; } = [];
}
