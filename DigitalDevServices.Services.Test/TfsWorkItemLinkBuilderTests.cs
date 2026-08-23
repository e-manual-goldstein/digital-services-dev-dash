using DigitalDevServices.Model.Tfs;

namespace DigitalDevServices.Services.Test;

[TestClass]
public sealed class TfsWorkItemLinkBuilderTests
{
    [TestMethod]
    public void BuildWorkItemUrl_ReplacesBuildNumberPlaceholder()
    {
        var url = TfsWorkItemLinkBuilder.BuildWorkItemUrl(
            "https://tfs.example.com/_workitems/edit/{BuildNumber}",
            "123456");

        Assert.AreEqual("https://tfs.example.com/_workitems/edit/123456", url);
    }

    [TestMethod]
    public void BuildWorkItemUrl_ReturnsNullWhenTemplateOrBuildNumberMissing()
    {
        Assert.IsNull(TfsWorkItemLinkBuilder.BuildWorkItemUrl(null, "123456"));
        Assert.IsNull(TfsWorkItemLinkBuilder.BuildWorkItemUrl("", "123456"));
        Assert.IsNull(TfsWorkItemLinkBuilder.BuildWorkItemUrl("https://tfs.example.com/{BuildNumber}", null));
        Assert.IsNull(TfsWorkItemLinkBuilder.BuildWorkItemUrl("https://tfs.example.com/{BuildNumber}", ""));
    }
}
