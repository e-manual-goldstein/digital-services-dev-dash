namespace DigitalDevServices.Model.Logs;

public sealed record ParsedLogException
{
    public string? Type { get; init; }

    public string? Message { get; init; }

    public string? StackTrace { get; init; }

    public ParsedLogException? InnerException { get; init; }

    public IEnumerable<ParsedLogException> EnumerateChain()
    {
        var current = this;
        while (current is not null)
        {
            yield return current;
            current = current.InnerException;
        }
    }
}
