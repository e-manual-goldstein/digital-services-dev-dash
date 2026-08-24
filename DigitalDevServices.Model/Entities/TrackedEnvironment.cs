namespace DigitalDevServices.Model.Entities;

/// <summary>
/// Local persistence record linking a DevDash identifier to a remote environment id.
/// Display data (name, SQL Server instance, etc.) is sourced from the remote Web API.
/// </summary>
public class TrackedEnvironment
{
    public Guid Id { get; set; }

    public int RemoteId { get; set; }

    public bool IsFavourite { get; set; }

    public DateTimeOffset DateLastUpdated { get; set; }
}
