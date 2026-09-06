using DigitalDevServices.Services.Configuration;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class XmlConfigurationFlattenerTests
{
    [TestMethod]
    public void Flatten_ImportsAppSettingsAndConnectionStrings()
    {
        var flattened = XmlConfigurationFlattener.Flatten("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Api:BaseUrl" value="https://example.com/api" />
              </appSettings>
              <connectionStrings>
                <add name="Default" connectionString="Server=localhost;" />
              </connectionStrings>
            </configuration>
            """);

        Assert.AreEqual("https://example.com/api", flattened["Api:BaseUrl"]);
        Assert.AreEqual("Server=localhost;", flattened["ConnectionStrings:Default"]);
    }
}
