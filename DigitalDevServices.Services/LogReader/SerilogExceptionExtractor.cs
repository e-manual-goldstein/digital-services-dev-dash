using System.Text.Json;
using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

internal static class SerilogExceptionExtractor
{
    private static readonly string[] ErrorStackTracePropertyNames = ["error.stack_trace", "error_stack_trace"];
    private static readonly string[] ErrorMessagePropertyNames = ["error.message", "error_message"];
    private static readonly string[] ErrorTypePropertyNames = ["error.type", "error_type"];

    public static ParsedLogException? TryExtract(JsonElement root)
    {
        if (TryGetString(root, "@x", out var legacyException))
        {
            return DotNetExceptionTextParser.TryParse(legacyException);
        }

        TryGetErrorValue(root, ErrorStackTracePropertyNames, out var stackTrace);
        TryGetErrorValue(root, ErrorMessagePropertyNames, out var errorMessage);
        TryGetErrorValue(root, ErrorTypePropertyNames, out var errorType);

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            var parsed = DotNetExceptionTextParser.TryParse(stackTrace);
            if (parsed is not null)
            {
                return parsed;
            }
        }

        if (string.IsNullOrWhiteSpace(errorMessage) && string.IsNullOrWhiteSpace(stackTrace))
        {
            return TryExtractNestedErrorObject(root);
        }

        var composed = ComposeExceptionText(errorType, errorMessage, stackTrace);
        return DotNetExceptionTextParser.TryParse(composed)
            ?? new ParsedLogException
            {
                Type = errorType,
                Message = errorMessage,
                StackTrace = stackTrace
            };
    }

    private static ParsedLogException? TryExtractNestedErrorObject(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var errorElement)
            || errorElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        TryGetString(errorElement, "stack_trace", out var stackTrace);
        TryGetString(errorElement, "message", out var errorMessage);
        TryGetString(errorElement, "type", out var errorType);

        if (string.IsNullOrWhiteSpace(stackTrace)
            && string.IsNullOrWhiteSpace(errorMessage)
            && string.IsNullOrWhiteSpace(errorType))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            var parsed = DotNetExceptionTextParser.TryParse(stackTrace);
            if (parsed is not null)
            {
                return parsed;
            }
        }

        var composed = ComposeExceptionText(errorType, errorMessage, stackTrace);
        return DotNetExceptionTextParser.TryParse(composed)
            ?? new ParsedLogException
            {
                Type = errorType,
                Message = errorMessage,
                StackTrace = stackTrace
            };
    }

    private static bool TryGetErrorValue(JsonElement root, IReadOnlyList<string> propertyNames, out string? value)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetString(root, propertyName, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string? ComposeExceptionText(string? type, string? message, string? stackTrace)
    {
        var header = !string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(message)
            ? $"{type}: {message}"
            : !string.IsNullOrWhiteSpace(message)
                ? message
                : type;

        if (string.IsNullOrWhiteSpace(header))
        {
            return stackTrace;
        }

        return string.IsNullOrWhiteSpace(stackTrace)
            ? header
            : $"{header}\n{stackTrace}";
    }

    private static bool TryGetString(JsonElement root, string propertyName, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return false;
        }

        value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.ToString()
        };

        return !string.IsNullOrWhiteSpace(value);
    }
}
