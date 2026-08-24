namespace DigitalDevServices.Model.Applications;

public sealed class LogPathTemplateResolutionResult
{
    public static LogPathTemplateResolutionResult Empty { get; } = new();

    public string ResolvedPath { get; init; } = string.Empty;

    public IReadOnlyList<string> UnknownTokens { get; init; } = [];
}
