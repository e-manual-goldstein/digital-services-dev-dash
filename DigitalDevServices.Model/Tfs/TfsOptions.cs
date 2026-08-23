namespace DigitalDevServices.Model.Tfs;

public class TfsOptions
{
    public const string SectionName = "Tfs";

    /// <summary>
    /// URL template for a TFS work item. Use {BuildNumber} for the work item id.
    /// </summary>
    public string WorkItemUrlTemplate { get; set; } = string.Empty;
}
