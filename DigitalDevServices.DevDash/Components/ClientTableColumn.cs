namespace DigitalDevServices.DevDash.Components;

using Microsoft.AspNetCore.Components;

public sealed class ClientTableColumn<TItem>
{
    public required string Title { get; init; }

    /// <summary>
    /// If provided, clicking the header will sort by this key.
    /// Keep it stable and preferably IComparable (string/int/DateTime/Guid/etc).
    /// </summary>
    public Func<TItem, IComparable> SortKey { get; init; }

    /// <summary>
    /// Content rendered in each row cell for this column.
    /// </summary>
    public required RenderFragment<TItem> CellTemplate { get; init; }

    /// <summary>
    /// Optional header title override (e.g., include an icon).
    /// </summary>
    public RenderFragment HeaderTemplate { get; init; }

    /// <summary>
    /// Optional content rendered above the header (e.g., a dropdown filter).
    /// Rendered outside of the sort button to allow interactive controls.
    /// </summary>
    public RenderFragment HeaderTopTemplate { get; init; }

    public string HeaderCssClass { get; init; }
    public string CellCssClass { get; init; }
    public bool IsSortable => SortKey is not null;
}


