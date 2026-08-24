using System.Net;
using DigitalDevServices.Model.Environments;
using DigitalDevServices.Services.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class RemoteEnvironmentApiHttpHandlerFactoryTests
{
    private static readonly Uri BaseUri = new("https://environments.example.com/");

    [TestMethod]
    public void Create_ReturnsPlainHandlerWhenNtlmDisabled()
    {
        var handler = RemoteEnvironmentApiHttpHandlerFactory.Create(new RemoteEnvironmentApiOptions
        {
            BaseUrl = BaseUri.ToString(),
            UseNtlmAuthentication = false
        });

        Assert.IsInstanceOfType(handler, typeof(SocketsHttpHandler));
        var socketsHandler = (SocketsHttpHandler)handler;
        Assert.IsNull(socketsHandler.Credentials);
    }

    [TestMethod]
    public void Create_UsesDefaultNetworkCredentialsWhenNtlmEnabled()
    {
        var handler = (SocketsHttpHandler)RemoteEnvironmentApiHttpHandlerFactory.Create(new RemoteEnvironmentApiOptions
        {
            BaseUrl = BaseUri.ToString(),
            UseNtlmAuthentication = true,
            UseDefaultCredentials = true
        });

        Assert.IsNotNull(handler.Credentials);
        var credentialCache = (CredentialCache)handler.Credentials!;
        var credentials = credentialCache.GetCredential(BaseUri, "NTLM");
        Assert.AreSame(CredentialCache.DefaultNetworkCredentials, credentials);
        Assert.IsTrue(handler.PreAuthenticate);
    }

    [TestMethod]
    public void Create_UsesConfiguredCredentialsWhenProvided()
    {
        var handler = (SocketsHttpHandler)RemoteEnvironmentApiHttpHandlerFactory.Create(new RemoteEnvironmentApiOptions
        {
            BaseUrl = BaseUri.ToString(),
            UseNtlmAuthentication = true,
            UseDefaultCredentials = false,
            Username = "svc-devdash",
            Password = "secret",
            Domain = "CORP"
        });

        var credentialCache = (CredentialCache)handler.Credentials!;
        var credential = (NetworkCredential)credentialCache.GetCredential(BaseUri, "NTLM")!;
        Assert.AreEqual("svc-devdash", credential.UserName);
        Assert.AreEqual("secret", credential.Password);
        Assert.AreEqual("CORP", credential.Domain);
    }
}
