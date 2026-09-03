using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Test;

[TestClass]
public class EnvironmentPickerDisplayTests
{
    [TestMethod]
    public void FormatOptionLabel_UsesCodeAndNameWithEmDash()
    {
        var environment = CreateEnvironment(code: "UAT-01", name: "UAT-01");

        var label = EnvironmentPickerDisplay.FormatOptionLabel(environment);

        Assert.AreEqual("UAT-01 — UAT-01", label);
    }

    [TestMethod]
    public void FormatOptionLabel_FallsBackToNameWhenCodeMissing()
    {
        var environment = CreateEnvironment(code: "", name: "Integration");

        var label = EnvironmentPickerDisplay.FormatOptionLabel(environment);

        Assert.AreEqual("Integration", label);
    }

    [TestMethod]
    public void OrderForPicker_PlacesFavouritesFirstThenDisplayOrderThenName()
    {
        var alpha = CreateEnvironment(code: "A", name: "Alpha", displayOrder: 5);
        var bravo = CreateEnvironment(code: "B", name: "Bravo", displayOrder: 1, isFavourite: true);
        var charlie = CreateEnvironment(code: "C", name: "Charlie", displayOrder: 0);
        var delta = CreateEnvironment(code: "D", name: "Delta", displayOrder: 2, isFavourite: true);

        var ordered = EnvironmentPickerDisplay.OrderForPicker([alpha, bravo, charlie, delta]);

        var orderedIds = ordered.Select(environment => environment.LocalId).ToList();
        CollectionAssert.AreEqual(
            new[] { bravo.LocalId, delta.LocalId, charlie.LocalId, alpha.LocalId },
            orderedIds);
    }

    private static CachedEnvironment CreateEnvironment(
        string code,
        string name,
        int displayOrder = 0,
        bool isFavourite = false)
    {
        return new CachedEnvironment
        {
            LocalId = Guid.NewGuid(),
            RemoteId = Random.Shared.Next(1, 1000),
            IsFavourite = isFavourite,
            DisplayOrder = displayOrder,
            Details = new RemoteEnvironmentDetails
            {
                Code = code,
                Name = name,
                EnvironmentType = "UAT"
            },
            DateLastUpdated = DateTimeOffset.UtcNow,
            IsFromCache = false
        };
    }
}
