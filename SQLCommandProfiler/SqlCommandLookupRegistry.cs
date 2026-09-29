using System.Text.Json;
using System.Text.Json.Serialization;

namespace SQLCommandProfiler;

internal sealed class SqlCommandLookupRegistry
{
    private readonly Dictionary<string, SqlCommandAccess> _byFullName;
    private readonly Dictionary<string, SqlCommandAccess> _byShortName;

    private SqlCommandLookupRegistry(
        Dictionary<string, SqlCommandAccess> byFullName,
        Dictionary<string, SqlCommandAccess> byShortName)
    {
        _byFullName = byFullName;
        _byShortName = byShortName;
    }

    public static SqlCommandLookupRegistry Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("SQL command lookup file was not found.", filePath);
        }

        var json = File.ReadAllText(filePath);
        var file = JsonSerializer.Deserialize<SqlCommandLookupFile>(json, JsonOptions)
            ?? throw new InvalidOperationException("SQL command lookup file could not be parsed.");

        var byFullName = new Dictionary<string, SqlCommandAccess>(StringComparer.OrdinalIgnoreCase);
        var byShortName = new Dictionary<string, SqlCommandAccess>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < file.Commands.Count; i++)
        {
            var entry = file.Commands[i];
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                throw new InvalidOperationException($"SQL command lookup entry at index {i} has an empty name.");
            }

            var normalized = NormalizeKey(entry.Name);
            byFullName[normalized] = entry.Access;

            var shortName = GetShortName(normalized);
            if (!byShortName.ContainsKey(shortName))
            {
                byShortName[shortName] = entry.Access;
            }
        }

        return new SqlCommandLookupRegistry(byFullName, byShortName);
    }

    public SqlCommandLookupMatch Match(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return new SqlCommandLookupMatch
            {
                ObjectName = string.Empty,
                IsKnown = false,
            };
        }

        var normalized = NormalizeKey(objectName);
        if (_byFullName.TryGetValue(normalized, out var fullAccess))
        {
            return new SqlCommandLookupMatch
            {
                ObjectName = objectName,
                Access = fullAccess,
                IsKnown = true,
            };
        }

        var shortName = GetShortName(normalized);
        if (_byShortName.TryGetValue(shortName, out var shortAccess))
        {
            return new SqlCommandLookupMatch
            {
                ObjectName = objectName,
                Access = shortAccess,
                IsKnown = true,
            };
        }

        return new SqlCommandLookupMatch
        {
            ObjectName = objectName,
            IsKnown = false,
        };
    }

    private static string NormalizeKey(string name)
    {
        return name.Trim().Trim('[', ']');
    }

    private static string GetShortName(string normalizedName)
    {
        var lastDot = normalizedName.LastIndexOf('.');
        return lastDot >= 0 ? normalizedName[(lastDot + 1)..] : normalizedName;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
