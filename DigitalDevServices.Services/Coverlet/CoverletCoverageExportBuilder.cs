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

    public static string BuildExportFileName(string? uploadedFileName)
    {
        if (string.IsNullOrWhiteSpace(uploadedFileName))
        {
            return "coverage-export.json";
        }

        var baseName = Path.GetFileNameWithoutExtension(uploadedFileName.Trim());
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return "coverage-export.json";
        }

        return $"{baseName}.filtered.json";
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
