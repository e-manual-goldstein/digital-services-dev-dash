using DigitalDevServices.Services.Configuration;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class ConfigurationConnectionStringKeyTests
{
    [TestMethod]
    public void IsConnectionStringKey_MatchesPrefixCaseInsensitively()
    {
        Assert.IsTrue(ConfigurationConnectionStringKey.IsConnectionStringKey("ConnectionStrings:Default"));
        Assert.IsTrue(ConfigurationConnectionStringKey.IsConnectionStringKey("connectionstrings:Audit"));
        Assert.IsFalse(ConfigurationConnectionStringKey.IsConnectionStringKey("FeatureFlags:NewCheckout"));
        Assert.IsFalse(ConfigurationConnectionStringKey.IsConnectionStringKey("ConnectionStrings"));
    }
}
