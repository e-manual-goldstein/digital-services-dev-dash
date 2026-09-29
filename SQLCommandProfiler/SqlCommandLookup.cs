using System.Text.Json.Serialization;

namespace SQLCommandProfiler;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SqlCommandAccess
{
    ReadOnly,
    ReadWrite,
}

public sealed class SqlCommandLookupMatch
{
    public required string ObjectName { get; init; }

    public SqlCommandAccess Access { get; init; }

    public bool IsKnown { get; init; }
}

public sealed class SqlCommandLookupFile
{
    public List<SqlCommandLookupEntry> Commands { get; init; } = [];
}

public sealed class SqlCommandLookupEntry
{
    public required string Name { get; init; }

    public required SqlCommandAccess Access { get; init; }
}
