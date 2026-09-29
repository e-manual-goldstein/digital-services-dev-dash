namespace SQLCommandProfiler;

public enum ProfilerEventBucket
{
    Insert,
    Update,
    ReadOnly,
}

public sealed class ProfilerLiveEventDisplay
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required ProfilerEventBucket Bucket { get; init; }

    public required string Title { get; init; }

    public string Subtitle { get; init; } = string.Empty;

    public required CapturedSqlEvent Captured { get; init; }
}

public sealed class SqlProfilerSessionOptions
{
    public string ApplicationName { get; init; } = "SQLCommandProfiler";

    public Action<ProfilerLiveEventDisplay>? OnLiveEvent { get; init; }

    public bool SuppressConsoleOutput { get; init; }
}
