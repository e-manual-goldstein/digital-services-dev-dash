using DigitalDevServices.Model.TextFormatting;

namespace DigitalDevServices.Services.TextFormatting;

public interface IFormattedTextService
{
    FormattedTextDisplayFormat? DetectAutoFormat(string? text);

    FormattedTextResult Format(string? text, FormattedTextDisplayFormat format);
}
