using System.Globalization;
using System.Text;
using System.Text.Json;
using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public static class CoverletCoverageExportBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Rows that match column filters and are not marked hidden (independent of the Show hidden toggle).
    /// </summary>
    public static IReadOnlyList<CoverletCoverageRow> SelectRowsForExport(
        IEnumerable<CoverletCoverageRow> rows,
        CoverletCoverageFilterState filters,
        IReadOnlySet<string> hiddenRowKeys)
    {
        return CoverletCoverageRowFilter
            .Apply(rows, filters)
            .Where(row => !hiddenRowKeys.Contains(row.RowKey))
            .ToList();
    }

    public static CoverletCoverageExportDocument BuildDocument(
        IReadOnlyList<CoverletCoverageRow> rows,
        string? sourceFileName) =>
        new()
        {
            ExportedAt = DateTimeOffset.UtcNow,
            SourceFileName = sourceFileName,
            RowCount = rows.Count,
            Rows = rows.Select(MapRow).ToList()
        };

    public static string ToJson(IReadOnlyList<CoverletCoverageRow> rows, string? sourceFileName) =>
        JsonSerializer.Serialize(BuildDocument(rows, sourceFileName), JsonOptions);

    public static string ToCsv(IReadOnlyList<CoverletCoverageRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CsvHeaderLine);

        foreach (var row in rows)
        {
            var exportRow = MapRow(row);
            builder.AppendJoin(
                ',',
                EscapeCsv(exportRow.Module),
                EscapeCsv(exportRow.SourceFile),
                EscapeCsv(exportRow.ClassName),
                EscapeCsv(exportRow.MethodName),
                exportRow.CoveredLines.ToString(CultureInfo.InvariantCulture),
                exportRow.CoverableLines.ToString(CultureInfo.InvariantCulture),
                exportRow.LineCoveragePercent.ToString(CultureInfo.InvariantCulture),
                exportRow.CoveredBranches.ToString(CultureInfo.InvariantCulture),
                exportRow.TotalBranches.ToString(CultureInfo.InvariantCulture),
                exportRow.BranchCoveragePercent.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    public static string BuildExportFileName(string? uploadedFileName, string extension = "json")
    {
        var normalizedExtension = string.IsNullOrWhiteSpace(extension)
            ? "json"
            : extension.TrimStart('.');

        if (string.IsNullOrWhiteSpace(uploadedFileName))
        {
            return $"coverage-export.{normalizedExtension}";
        }

        var baseName = Path.GetFileNameWithoutExtension(uploadedFileName.Trim());
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return $"coverage-export.{normalizedExtension}";
        }

        return $"{baseName}.filtered.{normalizedExtension}";
    }

    private const string CsvHeaderLine =
        "Module,SourceFile,ClassName,MethodName,CoveredLines,CoverableLines,LineCoveragePercent,CoveredBranches,TotalBranches,BranchCoveragePercent";

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains('"', StringComparison.Ordinal)
            && !value.Contains(',', StringComparison.Ordinal)
            && !value.Contains('\r', StringComparison.Ordinal)
            && !value.Contains('\n', StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static CoverletCoverageExportRow MapRow(CoverletCoverageRow row) =>
        new()
        {
            Module = row.Module,
            SourceFile = row.SourceFile,
            ClassName = row.ClassName,
            MethodName = row.MethodName,
            CoveredLines = row.CoveredLines,
            CoverableLines = row.CoverableLines,
            LineCoveragePercent = row.LineCoveragePercent,
            CoveredBranches = row.CoveredBranches,
            TotalBranches = row.TotalBranches,
            BranchCoveragePercent = row.BranchCoveragePercent
        };
}
