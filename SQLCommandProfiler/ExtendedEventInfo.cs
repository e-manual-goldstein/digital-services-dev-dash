namespace SQLCommandProfiler;

public readonly record struct ExtendedEventInfo(
    DateTimeOffset TimestampUtc,
    string EventName,
    string DatabaseName,
    string UserName,
    string ApplicationName,
    int SessionId,
    string SqlText,
    long DurationMicroseconds,
    long CpuMicroseconds,
    long LogicalReads,
    ulong QueryHash,
    ulong QueryPlanHash);
