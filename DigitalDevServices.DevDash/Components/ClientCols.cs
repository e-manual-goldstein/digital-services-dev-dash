using Microsoft.AspNetCore.Components;

namespace DigitalDevServices.DevDash.Components;

/// <summary>
/// Small helpers to make ClientTable column definitions less verbose in pages.
/// </summary>
public static class ClientCols
{
    public const string NowrapClass = "text-nowrap";

    public static ClientTableColumn<TItem> Col<TItem>(
        string title,
        RenderFragment<TItem> cell,
        Func<TItem, IComparable> sortKey = null,
        string headerCssClass = null,
        string cellCssClass = null,
        RenderFragment headerTemplate = null)
        => new()
        {
            Title = title,
            CellTemplate = cell,
            SortKey = sortKey,
            HeaderCssClass = headerCssClass,
            CellCssClass = cellCssClass,
            HeaderTemplate = headerTemplate
        };
}


