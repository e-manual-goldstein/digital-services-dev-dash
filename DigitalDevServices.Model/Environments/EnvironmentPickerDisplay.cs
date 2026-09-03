namespace DigitalDevServices.Model.Environments;

/// <summary>
/// Shared ordering and label formatting for environment picker dropdowns (ENV-020).
/// Label format: <c>CODE — Name</c> (em dash), e.g. <c>UAT-01 — UAT-01</c>.
/// </summary>
public static class EnvironmentPickerDisplay
{
    public static string FormatOptionLabel(CachedEnvironment environment)
    {
        var code = environment.Details.Code.Trim();
        var name = environment.Details.Name.Trim();

        if (code.Length > 0 && name.Length > 0)
        {
            return $"{code} — {name}";
        }

        if (name.Length > 0)
        {
            return name;
        }

        if (code.Length > 0)
        {
            return code;
        }

        return environment.RemoteId.ToString();
    }

    public static IReadOnlyList<CachedEnvironment> OrderForPicker(IEnumerable<CachedEnvironment> environments) =>
        environments
            .OrderByDescending(environment => environment.IsFavourite)
            .ThenBy(environment => environment.DisplayOrder)
            .ThenBy(environment => environment.Details.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
