namespace DigitalDevServices.DevDash.Services;

public enum ProfilerGroupSummaryDimension
{
    HostName,
    ApplicationName,
    Database,
    User,
}

public sealed class ProfilerGroupSummaryRow
{
    public required string GroupKey { get; init; }

    public int InsertCount { get; init; }

    public int UpdateCount { get; init; }

    public int DeleteCount { get; init; }

    public int ReadOnlyCount { get; init; }

    public int RecognisedCount { get; init; }

    public int UnknownCount { get; init; }

    public int TotalCount =>
        InsertCount + UpdateCount + DeleteCount + ReadOnlyCount + RecognisedCount + UnknownCount;
}

public static class ProfilerGroupSummaryDimensionLabels
{
    public static string GetColumnHeader(ProfilerGroupSummaryDimension dimension) =>
        dimension switch
        {
            ProfilerGroupSummaryDimension.HostName => "HostName",
            ProfilerGroupSummaryDimension.ApplicationName => "ApplicationName",
            ProfilerGroupSummaryDimension.Database => "Database",
            ProfilerGroupSummaryDimension.User => "User",
            _ => dimension.ToString(),
        };

    public static string GetDropdownLabel(ProfilerGroupSummaryDimension dimension) =>
        dimension switch
        {
            ProfilerGroupSummaryDimension.Database => "database",
            _ => GetColumnHeader(dimension),
        };
}
