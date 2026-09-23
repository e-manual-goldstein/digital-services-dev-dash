using DigitalDevServices.Model.Coverlet;

namespace DigitalDevServices.Services.Coverlet;

public interface ICoverletCoverageReportParser
{
    CoverletCoverageParseResult Parse(string json);
}
