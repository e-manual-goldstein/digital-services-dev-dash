using System.Xml;
using System.Xml.Linq;

namespace DigitalDevServices.Services.Configuration;

internal static class XmlConfigurationFlattener
{
    public static IReadOnlyDictionary<string, string> Flatten(string xmlContent)
    {
        try
        {
            var document = XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace);
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var add in document.Descendants("appSettings").Elements("add"))
            {
                var key = (string?)add.Attribute("key");
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                results[key.Trim()] = (string?)add.Attribute("value") ?? string.Empty;
            }

            foreach (var add in document.Descendants("connectionStrings").Elements("add"))
            {
                var name = (string?)add.Attribute("name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                results[$"ConnectionStrings:{name.Trim()}"] =
                    (string?)add.Attribute("connectionString") ?? string.Empty;
            }

            return results;
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
    }
}
