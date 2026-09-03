using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using DigitalDevServices.Model.TextFormatting;

namespace DigitalDevServices.Services.TextFormatting;

public sealed class FormattedTextService : IFormattedTextService
{
    private static readonly JsonSerializerOptions PrettyJsonOptions = new() { WriteIndented = true };

    public FormattedTextDisplayFormat? DetectAutoFormat(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        if (TryPrettyPrintJsonDocument(trimmed, out _))
        {
            return FormattedTextDisplayFormat.Json;
        }

        if (IsNdJson(trimmed))
        {
            return FormattedTextDisplayFormat.Json;
        }

        if (TryPrettyPrintXml(trimmed, out _))
        {
            return FormattedTextDisplayFormat.Xml;
        }

        return null;
    }

    public FormattedTextResult Format(string? text, FormattedTextDisplayFormat format)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new FormattedTextResult
            {
                DisplayText = string.Empty,
                IsFormatted = false
            };
        }

        return format switch
        {
            FormattedTextDisplayFormat.Json => FormatJson(text),
            FormattedTextDisplayFormat.Xml => FormatXml(text),
            _ => new FormattedTextResult
            {
                DisplayText = text,
                IsFormatted = false
            }
        };
    }

    private static FormattedTextResult FormatJson(string text)
    {
        var trimmed = text.Trim();
        if (TryPrettyPrintJsonDocument(trimmed, out var formattedDocument))
        {
            return new FormattedTextResult
            {
                DisplayText = formattedDocument,
                IsFormatted = true
            };
        }

        if (TryPrettyPrintNdJson(text, out var formattedLines, out var nonJsonLineCount))
        {
            return new FormattedTextResult
            {
                DisplayText = formattedLines,
                Hint = nonJsonLineCount > 0
                    ? "Some lines were left unchanged because they are not valid JSON."
                    : null,
                IsFormatted = true
            };
        }

        return new FormattedTextResult
        {
            DisplayText = text,
            Hint = "Not valid JSON.",
            IsFormatted = false
        };
    }

    private static FormattedTextResult FormatXml(string text)
    {
        var trimmed = text.Trim();
        if (TryPrettyPrintXml(trimmed, out var formatted))
        {
            return new FormattedTextResult
            {
                DisplayText = formatted,
                IsFormatted = true
            };
        }

        return new FormattedTextResult
        {
            DisplayText = text,
            Hint = "Not valid XML.",
            IsFormatted = false
        };
    }

    private static bool TryPrettyPrintJsonDocument(string text, out string formatted)
    {
        formatted = text;

        try
        {
            using var document = JsonDocument.Parse(text);
            formatted = JsonSerializer.Serialize(document.RootElement, PrettyJsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryPrettyPrintNdJson(string text, out string formatted, out int nonJsonLineCount)
    {
        formatted = string.Empty;
        nonJsonLineCount = 0;

        var lines = text.Split('\n');
        var builder = new StringBuilder(text.Length);
        var formattedAnyLine = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                builder.AppendLine();
                continue;
            }

            if (TryPrettyPrintJsonDocument(line, out var prettyLine))
            {
                builder.AppendLine(prettyLine);
                formattedAnyLine = true;
                continue;
            }

            builder.AppendLine(line);
            nonJsonLineCount++;
        }

        if (!formattedAnyLine)
        {
            return false;
        }

        formatted = builder.ToString().TrimEnd('\r', '\n');
        return true;
    }

    private static bool IsNdJson(string text)
    {
        var lines = text.Split('\n');
        var hasJsonLine = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (!TryPrettyPrintJsonDocument(trimmed, out _))
            {
                return false;
            }

            hasJsonLine = true;
        }

        return hasJsonLine;
    }

    private static bool TryPrettyPrintXml(string text, out string formatted)
    {
        formatted = text;

        try
        {
            var document = XDocument.Parse(text, LoadOptions.None);
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace
            };

            var builder = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(builder, settings))
            {
                document.Save(xmlWriter);
            }

            formatted = builder.ToString().TrimEnd();
            return formatted.Length > 0;
        }
        catch (XmlException)
        {
            return false;
        }
    }
}
