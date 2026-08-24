using System.Net;
using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

internal static class RemoteEnvironmentApiHttpHandlerFactory
{
    public static HttpMessageHandler Create(RemoteEnvironmentApiOptions options)
    {
        if (!options.UseNtlmAuthentication)
        {
            return new SocketsHttpHandler();
        }

        var baseUri = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        var credentialCache = new CredentialCache();
        credentialCache.Add(baseUri, "NTLM", CreateCredential(options));

        return new SocketsHttpHandler
        {
            Credentials = credentialCache,
            PreAuthenticate = true
        };
    }

    private static NetworkCredential CreateCredential(RemoteEnvironmentApiOptions options)
    {
        if (options.UseDefaultCredentials
            || (string.IsNullOrWhiteSpace(options.Username) && string.IsNullOrWhiteSpace(options.Password)))
        {
            return CredentialCache.DefaultNetworkCredentials;
        }

        return new NetworkCredential(
            options.Username,
            options.Password,
            string.IsNullOrWhiteSpace(options.Domain) ? null : options.Domain);
    }
}
