namespace SQLCommandProfiler;

public readonly record struct ExtendedEventInfo(
    DateTimeOffset TimestampUtc,
    string EventName,
    string ObjectName,
    string DatabaseName,
    string UserName,
    string ApplicationName,
    string HostName,
    int SessionId,
    int ClientProcessId,
    string SqlText,
    string Statement,
    long DurationMicroseconds,
    long CpuMicroseconds,
    long LogicalReads,
    ulong QueryHash,
    ulong QueryPlanHash);
