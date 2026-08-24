using DigitalDevServices.Model.Applications;

namespace DigitalDevServices.Services.Applications;

public interface ILogPathTemplateService
{
    LogPathTemplateResolutionResult Resolve(string? template, LogPathTemplateContext context);
}
