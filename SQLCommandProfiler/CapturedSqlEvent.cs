namespace SQLCommandProfiler;

internal sealed class CapturedSqlEvent
{
    public required ExtendedEventInfo Info { get; init; }

    public EfInterpretation? EfInterpretation { get; set; }
}
