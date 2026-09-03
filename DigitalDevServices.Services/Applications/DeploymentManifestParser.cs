using DigitalDevServices.Model.Applications;

namespace DigitalDevServices.Services.Applications;

public sealed class DeploymentManifestParseResult
{
    public IReadOnlyList<DeployedPackageInfo> Packages { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public bool CouldReadFile { get; init; }
}

public static class DeploymentManifestParser
{
    public const string ManifestFileName = "manifest.csv";

    public static DeploymentManifestParseResult ParseFile(string manifestPath)
    {
        string[] lines;

        try
        {
            lines = File.ReadAllLines(manifestPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new DeploymentManifestParseResult
            {
                CouldReadFile = false,
                Warnings = [$"Could not read '{ManifestFileName}': {ex.Message}"]
            };
        }

        return ParseLines(lines);
    }

    internal static DeploymentManifestParseResult ParseLines(IReadOnlyList<string> lines)
    {
        var packages = new List<DeployedPackageInfo>();
        var warnings = new List<string>();

        if (lines.Count == 0)
        {
            warnings.Add($"{ManifestFileName} is empty.");
            return new DeploymentManifestParseResult
            {
                CouldReadFile = true,
                Warnings = warnings
            };
        }

        for (var lineIndex = 1; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!TryParseManifestRow(line, out var representativePath, out var version))
            {
                warnings.Add($"Line {lineIndex + 1} could not be parsed: {line}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(representativePath))
            {
                warnings.Add($"Line {lineIndex + 1} is missing a representative file path.");
                continue;
            }

            representativePath = representativePath.Trim();
            version = version?.Trim();

            packages.Add(new DeployedPackageInfo
            {
                RepresentativePath = representativePath,
                FileName = Path.GetFileName(representativePath),
                AssemblyVersion = string.IsNullOrWhiteSpace(version) ? null : version
            });
        }

        return new DeploymentManifestParseResult
        {
            CouldReadFile = true,
            Packages = packages
                .OrderBy(package => package.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Warnings = warnings
        };
    }

    internal static bool TryParseManifestRow(string line, out string representativePath, out string? version)
    {
        representativePath = string.Empty;
        version = null;

        var fields = QuotedCsvParser.ParseFields(line);
        if (fields.Count != 2)
        {
            return false;
        }

        representativePath = fields[0];
        version = fields[1];
        return true;
    }
}

internal static class QuotedCsvParser
{
    public static IReadOnlyList<string> ParseFields(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (character == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        fields.Add(current.ToString());
        return fields;
    }
}
