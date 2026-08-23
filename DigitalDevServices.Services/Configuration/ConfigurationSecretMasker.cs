namespace DigitalDevServices.Services.Configuration;

public static class ConfigurationSecretMasker
{
    private static readonly string[] SensitiveKeyTokens = ["Secret", "Password", "Key"];

    public static bool ShouldMaskKey(string key) =>
        SensitiveKeyTokens.Any(token => key.Contains(token, StringComparison.OrdinalIgnoreCase));

    public static string GetDisplayValue(string key, string value, bool revealSecrets) =>
        !revealSecrets && ShouldMaskKey(key) ? "••••••••" : value;
}
