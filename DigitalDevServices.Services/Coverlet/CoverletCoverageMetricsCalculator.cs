using System.Text.Json;

namespace DigitalDevServices.Services.Coverlet;

internal static class CoverletCoverageMetricsCalculator
{
    public static (int CoveredLines, int CoverableLines, int TotalLines, decimal LineCoveragePercent) FromLines(
        JsonElement linesElement)
    {
        if (linesElement.ValueKind != JsonValueKind.Object)
        {
            return (0, 0, 0, 0);
        }

        var coverable = 0;
        var covered = 0;

        foreach (var line in linesElement.EnumerateObject())
        {
            coverable++;
            if (ReadHitCount(line.Value) > 0)
            {
                covered++;
            }
        }

        return (covered, coverable, coverable, Percent(covered, coverable));
    }

    public static (int CoveredBranches, int TotalBranches, decimal BranchCoveragePercent) FromBranches(
        JsonElement branchesElement)
    {
        if (branchesElement.ValueKind != JsonValueKind.Object)
        {
            return (0, 0, 0);
        }

        var covered = 0;
        var total = 0;

        foreach (var branch in branchesElement.EnumerateObject())
        {
            switch (branch.Value.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var entry in branch.Value.EnumerateArray())
                    {
                        total++;
                        if (ReadHitCount(entry) > 0)
                        {
                            covered++;
                        }
                    }

                    break;
                case JsonValueKind.Number:
                case JsonValueKind.String:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    total++;
                    if (ReadHitCount(branch.Value) > 0)
                    {
                        covered++;
                    }

                    break;
            }
        }

        return (covered, total, Percent(covered, total));
    }

    private static int ReadHitCount(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out var number) ? number : 0,
            JsonValueKind.String => int.TryParse(value.GetString(), out var parsed) ? parsed : 0,
            JsonValueKind.True => 1,
            _ => 0
        };

    private static decimal Percent(int covered, int total) =>
        total == 0 ? 0 : Math.Round(covered * 100m / total, 2);
}
