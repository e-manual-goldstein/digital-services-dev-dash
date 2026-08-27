namespace DigitalDevServices.Model.Logs;

public sealed class LogPathResolutionResult
{
    public bool IsSuccess { get; init; }

    public string? LogPath { get; init; }

    public bool RefreshedEnvironment { get; init; }

    public string? ErrorMessage { get; init; }

    public IReadOnlyList<string> UnknownTokens { get; init; } = [];
}
