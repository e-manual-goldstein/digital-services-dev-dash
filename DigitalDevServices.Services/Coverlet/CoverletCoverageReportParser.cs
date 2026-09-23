using System.Text.Json;
using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public sealed class CoverletCoverageReportParser : ICoverletCoverageReportParser
{
    public CoverletCoverageParseResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Error("The file is empty.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return ParseRoot(document.RootElement);
        }
        catch (JsonException ex)
        {
            return Error($"Invalid JSON: {ex.Message}");
        }
    }

    private static CoverletCoverageParseResult ParseRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Error("Unrecognized Coverlet JSON: root must be an object.");
        }

        var rows = new List<CoverletCoverageRow>();

        foreach (var moduleProperty in root.EnumerateObject())
        {
            if (!TryParseModule(moduleProperty.Name, moduleProperty.Value, rows))
            {
                continue;
            }
        }

        if (rows.Count == 0)
        {
            return Error("Unrecognized Coverlet JSON: no coverage modules or methods were found.");
        }

        return new CoverletCoverageParseResult
        {
            Rows = rows,
            Summary = BuildAggregateSummary(rows)
        };
    }

    private static bool TryParseModule(string moduleName, JsonElement moduleElement, List<CoverletCoverageRow> rows)
    {
        if (moduleElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!moduleElement.TryGetProperty("Classes", out var classesElement)
            && !TryGetPropertyIgnoreCase(moduleElement, "Classes", out classesElement))
        {
            return false;
        }

        if (classesElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var classProperty in classesElement.EnumerateObject())
        {
            ParseClass(moduleName, classProperty.Name, classProperty.Value, rows);
        }

        return true;
    }

    private static void ParseClass(
        string moduleName,
        string className,
        JsonElement classElement,
        List<CoverletCoverageRow> rows)
    {
        if (classElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (!classElement.TryGetProperty("Methods", out var methodsElement)
            && !TryGetPropertyIgnoreCase(classElement, "Methods", out methodsElement))
        {
            return;
        }

        if (methodsElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var methodProperty in methodsElement.EnumerateObject())
        {
            var summary = ReadSummary(methodProperty.Value);
            if (summary is null)
            {
                continue;
            }

            rows.Add(new CoverletCoverageRow
            {
                RowKey = BuildRowKey(moduleName, className, methodProperty.Name),
                Module = moduleName,
                ClassName = className,
                MethodName = methodProperty.Name,
                CoveredLines = summary.CoveredLines,
                CoverableLines = summary.CoverableLines,
                TotalLines = summary.TotalLines,
                LineCoveragePercent = summary.LineCoveragePercent,
                CoveredBranches = summary.CoveredBranches,
                TotalBranches = summary.TotalBranches,
                BranchCoveragePercent = summary.BranchCoveragePercent
            });
        }
    }

    private static CoverletCoverageSummary? ReadSummary(JsonElement methodOrSummaryContainer)
    {
        if (methodOrSummaryContainer.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!methodOrSummaryContainer.TryGetProperty("Summary", out var summaryElement)
            && !TryGetPropertyIgnoreCase(methodOrSummaryContainer, "Summary", out summaryElement))
        {
            return null;
        }

        return ReadSummaryValues(summaryElement);
    }

    private static CoverletCoverageSummary? ReadSummaryValues(JsonElement summaryElement)
    {
        if (summaryElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new CoverletCoverageSummary
        {
            CoveredLines = ReadInt(summaryElement, "coveredlines"),
            CoverableLines = ReadInt(summaryElement, "coverablelines"),
            TotalLines = ReadInt(summaryElement, "totallines"),
            LineCoveragePercent = ReadDecimal(summaryElement, "linecoverage"),
            CoveredBranches = ReadInt(summaryElement, "coveredbranches"),
            TotalBranches = ReadInt(summaryElement, "totalbranches"),
            BranchCoveragePercent = ReadDecimal(summaryElement, "branchcoverage"),
            CoveredMethods = ReadInt(summaryElement, "coveredmethods"),
            TotalMethods = ReadInt(summaryElement, "totalmethods"),
            MethodCoveragePercent = ReadDecimal(summaryElement, "methodcoverage")
        };
    }

    private static CoverletCoverageSummary BuildAggregateSummary(IReadOnlyList<CoverletCoverageRow> rows)
    {
        var coveredLines = rows.Sum(row => row.CoveredLines);
        var coverableLines = rows.Sum(row => row.CoverableLines);
        var totalLines = rows.Sum(row => row.TotalLines);
        var coveredBranches = rows.Sum(row => row.CoveredBranches);
        var totalBranches = rows.Sum(row => row.TotalBranches);
        var coveredMethods = rows.Count(row => row.CoveredLines > 0 || row.LineCoveragePercent > 0);
        var totalMethods = rows.Count;

        return new CoverletCoverageSummary
        {
            CoveredLines = coveredLines,
            CoverableLines = coverableLines,
            TotalLines = totalLines,
            LineCoveragePercent = Percent(coveredLines, coverableLines),
            CoveredBranches = coveredBranches,
            TotalBranches = totalBranches,
            BranchCoveragePercent = Percent(coveredBranches, totalBranches),
            CoveredMethods = coveredMethods,
            TotalMethods = totalMethods,
            MethodCoveragePercent = Percent(coveredMethods, totalMethods)
        };
    }

    private static decimal Percent(int covered, int total) =>
        total == 0 ? 0 : Math.Round(covered * 100m / total, 2);

    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out var number) ? number : 0,
            JsonValueKind.String => int.TryParse(value.GetString(), out var parsed) ? parsed : 0,
            _ => 0
        };
    }

    private static decimal ReadDecimal(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetDecimal(out var number) ? number : 0,
            JsonValueKind.String => decimal.TryParse(value.GetString(), out var parsed) ? parsed : 0,
            _ => 0
        };
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string BuildRowKey(string module, string className, string methodName) =>
        $"{module}\u001f{className}\u001f{methodName}";

    private static CoverletCoverageParseResult Error(string message) =>
        new() { ErrorMessage = message };
}
