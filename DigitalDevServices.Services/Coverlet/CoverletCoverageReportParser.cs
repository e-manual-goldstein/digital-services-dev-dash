using System.Text.Json;
using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public sealed class CoverletCoverageReportParser : ICoverletCoverageReportParser
{
    private static readonly string[] ModuleMetadataPropertyNames = ["Summary", "Classes"];

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
            if (moduleProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (HasPropertyIgnoreCase(moduleProperty.Value, "Classes"))
            {
                ParseModernModule(moduleProperty.Name, moduleProperty.Value, rows);
            }
            else
            {
                ParseLegacyModule(moduleProperty.Name, moduleProperty.Value, rows);
            }
        }

        if (rows.Count == 0)
        {
            return Error("Unrecognized Coverlet JSON: no coverage modules or methods were found.");
        }

        return new CoverletCoverageParseResult
        {
            Rows = rows,
            Summary = CoverletCoverageSummaryCalculator.BuildFromRows(rows)
        };
    }

    private static void ParseModernModule(string moduleName, JsonElement moduleElement, List<CoverletCoverageRow> rows)
    {
        if (!TryGetPropertyIgnoreCase(moduleElement, "Classes", out var classesElement)
            || classesElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var classProperty in classesElement.EnumerateObject())
        {
            ParseModernClass(moduleName, classProperty.Name, classProperty.Value, rows);
        }
    }

    private static void ParseLegacyModule(string moduleName, JsonElement moduleElement, List<CoverletCoverageRow> rows)
    {
        foreach (var fileProperty in moduleElement.EnumerateObject())
        {
            if (IsMetadataProperty(fileProperty.Name))
            {
                continue;
            }

            if (fileProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            ParseLegacyFile(moduleName, fileProperty.Name, fileProperty.Value, rows);
        }
    }

    private static void ParseLegacyFile(
        string moduleName,
        string sourceFile,
        JsonElement fileElement,
        List<CoverletCoverageRow> rows)
    {
        foreach (var typeProperty in fileElement.EnumerateObject())
        {
            if (IsMetadataProperty(typeProperty.Name))
            {
                continue;
            }

            if (typeProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            ParseLegacyType(moduleName, sourceFile, typeProperty.Name, typeProperty.Value, rows);
        }
    }

    private static void ParseLegacyType(
        string moduleName,
        string sourceFile,
        string typeName,
        JsonElement typeElement,
        List<CoverletCoverageRow> rows)
    {
        foreach (var methodProperty in typeElement.EnumerateObject())
        {
            if (IsMetadataProperty(methodProperty.Name))
            {
                continue;
            }

            if (methodProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryGetPropertyIgnoreCase(methodProperty.Value, "Lines", out var linesElement))
            {
                continue;
            }

            var lineMetrics = CoverletCoverageMetricsCalculator.FromLines(linesElement);
            var branchMetrics = TryGetPropertyIgnoreCase(methodProperty.Value, "Branches", out var branchesElement)
                ? CoverletCoverageMetricsCalculator.FromBranches(branchesElement)
                : (CoveredBranches: 0, TotalBranches: 0, BranchCoveragePercent: 0m);

            rows.Add(new CoverletCoverageRow
            {
                RowKey = BuildRowKey(moduleName, sourceFile, typeName, methodProperty.Name),
                Module = moduleName,
                SourceFile = sourceFile,
                ClassName = typeName,
                MethodName = methodProperty.Name,
                CoveredLines = lineMetrics.CoveredLines,
                CoverableLines = lineMetrics.CoverableLines,
                TotalLines = lineMetrics.TotalLines,
                LineCoveragePercent = lineMetrics.LineCoveragePercent,
                CoveredBranches = branchMetrics.CoveredBranches,
                TotalBranches = branchMetrics.TotalBranches,
                BranchCoveragePercent = branchMetrics.BranchCoveragePercent
            });
        }
    }

    private static void ParseModernClass(
        string moduleName,
        string className,
        JsonElement classElement,
        List<CoverletCoverageRow> rows)
    {
        if (classElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (!TryGetPropertyIgnoreCase(classElement, "Methods", out var methodsElement)
            || methodsElement.ValueKind != JsonValueKind.Object)
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
                RowKey = BuildRowKey(moduleName, string.Empty, className, methodProperty.Name),
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

        if (!TryGetPropertyIgnoreCase(methodOrSummaryContainer, "Summary", out var summaryElement))
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

    private static bool IsMetadataProperty(string propertyName) =>
        ModuleMetadataPropertyNames.Any(name =>
            string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase));

    private static bool HasPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

    private static string BuildRowKey(string module, string sourceFile, string className, string methodName) =>
        $"{module}\u001f{sourceFile}\u001f{className}\u001f{methodName}";

    private static CoverletCoverageParseResult Error(string message) =>
        new() { ErrorMessage = message };
}
