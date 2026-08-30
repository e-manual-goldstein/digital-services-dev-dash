using System.Text.RegularExpressions;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

public static partial class DotNetExceptionTextParser
{
    private const string InnerExceptionMarker = "--- End of inner exception stack trace ---";

    [GeneratedRegex(@"^\s*--->\s*", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex InnerExceptionSplitPattern();

    [GeneratedRegex(
        @"^(?<type>[\w.]+(?:\s*\([^)]*\))?)\s*:\s*(?<message>.*)$",
        RegexOptions.Compiled)]
    private static partial Regex ExceptionHeaderPattern();

    public static ParsedLogException? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Replace("\r\n", "\n").Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        var segments = SplitInnerExceptionSegments(normalized);
        ParsedLogException? inner = null;

        for (var index = segments.Count - 1; index >= 0; index--)
        {
            var segment = ParseSegment(segments[index]);
            if (segment is null)
            {
                continue;
            }

            if (inner is not null)
            {
                segment = segment with { InnerException = inner };
            }

            inner = segment;
        }

        return inner;
    }

    private static List<string> SplitInnerExceptionSegments(string text)
    {
        var withoutMarkers = text.Replace(InnerExceptionMarker, string.Empty, StringComparison.Ordinal);
        return InnerExceptionSplitPattern()
            .Split(withoutMarkers)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(segment => segment.Trim())
            .ToList();
    }

    private static ParsedLogException? ParseSegment(string segment)
    {
        var lines = segment.Split('\n', StringSplitOptions.None);
        if (lines.Length == 0)
        {
            return null;
        }

        var header = lines[0].Trim();
        var stackTrace = lines.Length > 1
            ? string.Join('\n', lines.Skip(1).Select(line => line.TrimEnd('\r'))).Trim()
            : null;

        var headerMatch = ExceptionHeaderPattern().Match(header);
        if (headerMatch.Success)
        {
            return new ParsedLogException
            {
                Type = headerMatch.Groups["type"].Value.Trim(),
                Message = headerMatch.Groups["message"].Value.Trim(),
                StackTrace = string.IsNullOrWhiteSpace(stackTrace) ? null : stackTrace
            };
        }

        if (IsStackTraceOnly(segment))
        {
            return new ParsedLogException
            {
                StackTrace = segment.Trim()
            };
        }

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            return new ParsedLogException
            {
                Message = header,
                StackTrace = stackTrace
            };
        }

        return new ParsedLogException
        {
            Message = header
        };
    }

    private static bool IsStackTraceOnly(string segment)
    {
        foreach (var line in segment.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("at ", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return segment.Split('\n').Any(line => !string.IsNullOrWhiteSpace(line));
    }
}
