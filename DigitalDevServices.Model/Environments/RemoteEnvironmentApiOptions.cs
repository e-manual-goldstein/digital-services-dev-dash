namespace DigitalDevServices.Model.Environments;

public class RemoteEnvironmentApiOptions
{
    public const string SectionName = "RemoteEnvironmentApi";

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// When true, outbound requests authenticate to the API with NTLM.
    /// </summary>
    public bool UseNtlmAuthentication { get; set; }

    /// <summary>
    /// Use the application process Windows identity. When false, set <see cref="Username"/> and <see cref="Password"/>.
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Domain { get; set; }

    /// <summary>
    /// Relative path template for a single environment. Use {id} for the remote id.
    /// </summary>
    public string GetEnvironmentPath { get; set; } = "api/environments/{id}";

    /// <summary>
    /// Relative path for listing all environments.
    /// </summary>
    public string ListEnvironmentsPath { get; set; } = "api/environments";
}
