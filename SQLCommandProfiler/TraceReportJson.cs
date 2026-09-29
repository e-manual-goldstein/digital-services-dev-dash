using System.Text.Json;
using System.Text.Json.Serialization;

namespace SQLCommandProfiler;

internal static class TraceReportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string Serialize(TraceReportDocument document)
    {
        return JsonSerializer.Serialize(document, Options);
    }
}
