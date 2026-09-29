using System.Text.Json.Serialization;

namespace SQLCommandProfiler;

public sealed class TraceReportDocument
{
    public required string TraceSessionName { get; init; }

    public DateTimeOffset GeneratedAtUtc { get; init; }

    public required TraceReportSummary Summary { get; init; }

    public required EfCommandReport EfCommandReport { get; init; }

    public required KnownSqlCommandReport KnownSqlCommandReport { get; init; }

    public required IReadOnlyList<ApplicationCommandGroup> CommandsByApplicationName { get; init; }
}

public sealed class KnownSqlCommandReport
{
    public bool LookupConfigured { get; init; }

    public int RpcEventsWithObjectName { get; init; }

    public int KnownReadOnly { get; init; }

    public int KnownReadWrite { get; init; }

    public int UnknownCommands { get; init; }

    public IReadOnlyDictionary<string, int> InvocationsByCommand { get; init; } = new Dictionary<string, int>();
}

public sealed class TraceReportSummary
{
    public int TotalEventsCaptured { get; init; }

    public int BatchCount { get; init; }

    public IReadOnlyDictionary<string, int> EventsByType { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> EventsByDatabase { get; init; } = new Dictionary<string, int>();
}

public sealed class EfCommandReport
{
    public bool InterpretationEnabled { get; init; }

    public int Ef6CommandsIdentified { get; init; }

    public int EntitiesCreated { get; init; }

    public int EntitiesUpdated { get; init; }

    public int EntitiesDeleted { get; init; }

    public int ReadOnlyCommands { get; init; }

    public int UnclassifiedOrNonEfCommands { get; init; }

    public IReadOnlyDictionary<string, int> CreatesByTable { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> UpdatesByTable { get; init; } = new Dictionary<string, int>();
}

public sealed class ApplicationCommandGroup
{
    public required string ApplicationName { get; init; }

    public int EventCount { get; init; }

    public IReadOnlyDictionary<string, int> EventsByType { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<CapturedEventSummary> Events { get; init; } = [];
}

public sealed class CapturedEventSummary
{
    public DateTimeOffset TimestampUtc { get; init; }

    public required string EventName { get; init; }

    public string? ObjectName { get; init; }

    public required string DatabaseName { get; init; }

    public string? UserName { get; init; }

    public string? HostName { get; init; }

    public long DurationMicroseconds { get; init; }

    public string? SqlSnippet { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EfSqlAccess? EfAccess { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SqlCommandAccess? KnownCommandAccess { get; init; }

    public bool? IsKnownCommand { get; init; }
}
