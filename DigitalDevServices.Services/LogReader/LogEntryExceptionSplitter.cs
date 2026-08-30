using DigitalDevServices.Model.Logs;

namespace DigitalDevServices.Services.Logs;

internal static class LogEntryExceptionSplitter
{
    public static (string Message, ParsedLogException? Exception) Split(string fullMessage)
    {
        if (string.IsNullOrWhiteSpace(fullMessage))
        {
            return (fullMessage, null);
        }

        var newlineIndex = fullMessage.IndexOf('\n');
        if (newlineIndex < 0)
        {
            return (fullMessage, null);
        }

        var summary = fullMessage[..newlineIndex].TrimEnd('\r');
        var remainder = fullMessage[(newlineIndex + 1)..].TrimStart('\r', '\n');
        if (!LooksLikeExceptionBlock(remainder))
        {
            return (fullMessage, null);
        }

        var exception = DotNetExceptionTextParser.TryParse(remainder);
        return exception is null
            ? (fullMessage, null)
            : (summary, exception);
    }

    private static bool LooksLikeExceptionBlock(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var firstLine = text.Split('\n')[0].Trim();
        if (firstLine.StartsWith("--->", StringComparison.Ordinal))
        {
            return true;
        }

        if (ExceptionHeaderLooksLikeDotNetType(firstLine))
        {
            return true;
        }

        return text.Contains("\n   at ", StringComparison.Ordinal)
            || text.Contains("\nat ", StringComparison.Ordinal);
    }

    private static bool ExceptionHeaderLooksLikeDotNetType(string firstLine)
    {
        var colonIndex = firstLine.IndexOf(':');
        if (colonIndex <= 0)
        {
            return false;
        }

        var typePart = firstLine[..colonIndex].Trim();
        return typePart.Contains('.')
            && !typePart.Contains(' ')
            && char.IsLetter(typePart[0]);
    }
}
