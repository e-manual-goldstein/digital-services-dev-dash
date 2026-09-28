using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

internal static class EfSqlInterpretationOptions
{
    public const string ConfigKey = "EfSqlInterpretation";

    public static EfSqlInterpretationWhen ReadFrom(IConfigurationSection profilerSection)
    {
        var raw = profilerSection[ConfigKey] ?? nameof(EfSqlInterpretationWhen.Never);

        if (!Enum.TryParse(raw, ignoreCase: true, out EfSqlInterpretationWhen when))
        {
            throw new InvalidOperationException(
                $"Profiler:{ConfigKey} \"{raw}\" is invalid. Use Never, OnReceive, or OnReport.");
        }

        return when;
    }
}
