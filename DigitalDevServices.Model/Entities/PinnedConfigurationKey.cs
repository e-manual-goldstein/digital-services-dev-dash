namespace DigitalDevServices.Model.Entities;

public class PinnedConfigurationKey
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
