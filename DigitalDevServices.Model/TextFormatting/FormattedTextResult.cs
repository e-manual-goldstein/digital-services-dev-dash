namespace DigitalDevServices.Model.TextFormatting;

public sealed class FormattedTextResult
{
    public required string DisplayText { get; init; }

    public string? Hint { get; init; }

    public bool IsFormatted { get; init; }
}
