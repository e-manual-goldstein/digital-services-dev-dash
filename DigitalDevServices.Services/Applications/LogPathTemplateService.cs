using System.Text.RegularExpressions;
using DigitalDevServices.Model.Applications;

namespace DigitalDevServices.Services.Applications;

public sealed partial class LogPathTemplateService : ILogPathTemplateService
{
    public LogPathTemplateResolutionResult Resolve(string? template, LogPathTemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(template))
        {
            return LogPathTemplateResolutionResult.Empty;
        }

        var unknownTokens = new List<string>();
        var resolved = TokenPattern().Replace(
            template,
            match =>
            {
                var tokenName = match.Groups[1].Value;
                if (!TryGetTokenValue(tokenName, context, out var value))
                {
                    unknownTokens.Add(tokenName);
                    return match.Value;
                }

                return value ?? string.Empty;
            });

        return new LogPathTemplateResolutionResult
        {
            ResolvedPath = resolved,
            UnknownTokens = unknownTokens
        };
    }

    private static bool TryGetTokenValue(
        string tokenName,
        LogPathTemplateContext context,
        out string? value)
    {
        if (string.Equals(tokenName, LogPathTemplateTokens.AppName, StringComparison.OrdinalIgnoreCase))
        {
            value = context.AppName;
            return true;
        }

        if (string.Equals(tokenName, LogPathTemplateTokens.EnvironmentCode, StringComparison.OrdinalIgnoreCase))
        {
            value = context.EnvironmentCode;
            return true;
        }

        if (string.Equals(tokenName, LogPathTemplateTokens.EnvironmentName, StringComparison.OrdinalIgnoreCase))
        {
            value = context.EnvironmentName;
            return true;
        }

        if (string.Equals(tokenName, LogPathTemplateTokens.MachineName, StringComparison.OrdinalIgnoreCase))
        {
            value = context.MachineName;
            return true;
        }

        if (string.Equals(tokenName, LogPathTemplateTokens.ApplicationPoolName, StringComparison.OrdinalIgnoreCase))
        {
            value = context.ApplicationPoolName;
            return true;
        }

        if (string.Equals(tokenName, LogPathTemplateTokens.VirtualPath, StringComparison.OrdinalIgnoreCase))
        {
            value = context.VirtualPath;
            return true;
        }

        if (string.Equals(tokenName, LogPathTemplateTokens.PhysicalPath, StringComparison.OrdinalIgnoreCase))
        {
            value = context.PhysicalPath;
            return true;
        }

        value = null;
        return false;
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
