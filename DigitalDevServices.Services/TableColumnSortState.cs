namespace DigitalDevServices.Services;

public sealed class TableColumnSortState
{
    private string? _activeColumn;

    public string? ActiveColumn => _activeColumn;

    public bool IsDescending { get; private set; }

    public void CycleColumn(string columnKey)
    {
        if (string.IsNullOrEmpty(columnKey))
        {
            return;
        }

        if (!string.Equals(_activeColumn, columnKey, StringComparison.Ordinal))
        {
            _activeColumn = columnKey;
            IsDescending = false;
            return;
        }

        if (!IsDescending)
        {
            IsDescending = true;
            return;
        }

        _activeColumn = null;
        IsDescending = false;
    }

    public string IndicatorFor(string columnKey)
    {
        if (!string.Equals(_activeColumn, columnKey, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return IsDescending ? "▼" : "▲";
    }
}

public static class TableColumnSort
{
    public static IReadOnlyList<T> Apply<T>(
        IEnumerable<T> source,
        TableColumnSortState state,
        IReadOnlyDictionary<string, Func<T, IComparable>> columns)
    {
        if (state.ActiveColumn is null
            || !columns.TryGetValue(state.ActiveColumn, out var keySelector))
        {
            return source is IReadOnlyList<T> list ? list : source.ToList();
        }

        return state.IsDescending
            ? source.OrderByDescending(keySelector, NullSafeComparableComparer.Instance).ToList()
            : source.OrderBy(keySelector, NullSafeComparableComparer.Instance).ToList();
    }

    private sealed class NullSafeComparableComparer : IComparer<IComparable>
    {
        public static readonly NullSafeComparableComparer Instance = new();

        public int Compare(IComparable? x, IComparable? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            return x.CompareTo(y);
        }
    }
}
