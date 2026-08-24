namespace DigitalDevServices.Model.Logs;

public sealed class SampleLogDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string FileName { get; init; }

    public required string FormatName { get; init; }

    public string? Description { get; init; }
}
