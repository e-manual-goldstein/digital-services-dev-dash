using DigitalDevServices.Services.Configuration;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class ConfigurationSecretMaskerTests
{
    [TestMethod]
    public void ShouldMaskKey_MatchesSecretPasswordAndKeyTokens()
    {
        Assert.IsTrue(ConfigurationSecretMasker.ShouldMaskKey("Api:ClientSecret"));
        Assert.IsTrue(ConfigurationSecretMasker.ShouldMaskKey("ConnectionStrings:DefaultPassword"));
        Assert.IsTrue(ConfigurationSecretMasker.ShouldMaskKey("Security:SigningKey"));
        Assert.IsFalse(ConfigurationSecretMasker.ShouldMaskKey("FeatureFlags:NewCheckout"));
        Assert.IsFalse(ConfigurationSecretMasker.ShouldMaskKey("ConnectionStrings:Default"));
    }

    [TestMethod]
    public void GetDisplayValue_MasksUnlessRevealRequested()
    {
        Assert.AreEqual("••••••••", ConfigurationSecretMasker.GetDisplayValue("Api:ClientSecret", "super-secret", revealSecrets: false));
        Assert.AreEqual("super-secret", ConfigurationSecretMasker.GetDisplayValue("Api:ClientSecret", "super-secret", revealSecrets: true));
        Assert.AreEqual("true", ConfigurationSecretMasker.GetDisplayValue("FeatureFlags:Beta", "true", revealSecrets: false));
    }
}
