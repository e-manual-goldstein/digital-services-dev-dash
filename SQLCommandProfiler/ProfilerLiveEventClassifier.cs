namespace SQLCommandProfiler;

public static class ProfilerLiveEventClassifier
{
    public static bool TryCreateDisplay(CapturedSqlEvent captured, out ProfilerLiveEventDisplay display)
    {
        display = null!;

        if (TryFromEfInterpretation(captured, out display))
        {
            return true;
        }

        if (TryFromCommandLookup(captured, out display))
        {
            return true;
        }

        return false;
    }

    public static ProfilerLiveEventDisplay CreateUnknownDisplay(CapturedSqlEvent captured)
    {
        var info = captured.Info;
        string title;
        string subtitle;

        if (BuiltInSqlServerRpc.IsSpExecuteSql(info.ObjectName)
            && !string.IsNullOrWhiteSpace(info.SqlText))
        {
            title = ProfilerSqlSnippet.FormatForTile(info.SqlText);
            subtitle = string.IsNullOrWhiteSpace(info.DatabaseName)
                ? info.ApplicationName
                : info.DatabaseName;
        }
        else
        {
            title = !string.IsNullOrWhiteSpace(info.ObjectName)
                ? GetShortObjectName(info.ObjectName)
                : info.EventName;

            subtitle = string.IsNullOrWhiteSpace(info.DatabaseName)
                ? info.ApplicationName
                : info.DatabaseName;
        }

        return new ProfilerLiveEventDisplay
        {
            Bucket = ProfilerEventBucket.Unknown,
            Title = title,
            Subtitle = subtitle,
            Captured = captured,
        };
    }

    private static bool TryFromEfInterpretation(CapturedSqlEvent captured, out ProfilerLiveEventDisplay display)
    {
        display = null!;
        var interpretation = captured.EfInterpretation;
        if (interpretation is not { IsLikelyEf6: true })
        {
            return false;
        }

        var insert = interpretation.Statements.FirstOrDefault(statement => statement.Kind == EfSqlStatementKind.Insert);
        if (insert?.Insert is not null)
        {
            display = CreateTableEvent(ProfilerEventBucket.Insert, insert.Insert.Table, captured);
            return true;
        }

        var update = interpretation.Statements.FirstOrDefault(statement => statement.Kind == EfSqlStatementKind.Update);
        if (update?.Update is not null)
        {
            display = CreateTableEvent(ProfilerEventBucket.Update, update.Update.Table, captured);
            return true;
        }

        if (interpretation.Access == EfSqlAccess.ReadOnly)
        {
            display = CreateReadOnlyDatabaseEvent(captured);
            return true;
        }

        return false;
    }

    private static bool TryFromCommandLookup(CapturedSqlEvent captured, out ProfilerLiveEventDisplay display)
    {
        display = null!;
        var lookup = captured.CommandLookup;
        if (lookup is not { IsKnown: true })
        {
            return false;
        }

        var title = GetShortObjectName(lookup.ObjectName);
        var subtitle = string.IsNullOrWhiteSpace(captured.Info.DatabaseName)
            ? lookup.ObjectName
            : captured.Info.DatabaseName;

        display = new ProfilerLiveEventDisplay
        {
            Bucket = ProfilerEventBucket.RecognisedCommand,
            Title = title,
            Subtitle = FormatRecognisedCommandSubtitle(lookup, captured),
            Captured = captured,
        };

        return true;
    }

    private static string FormatRecognisedCommandSubtitle(
        SqlCommandLookupMatch lookup,
        CapturedSqlEvent captured)
    {
        if (!string.IsNullOrWhiteSpace(captured.Info.DatabaseName))
        {
            return captured.Info.DatabaseName;
        }

        return lookup.ObjectName;
    }

    private static ProfilerLiveEventDisplay CreateTableEvent(
        ProfilerEventBucket bucket,
        EfTableReference table,
        CapturedSqlEvent captured)
    {
        return new ProfilerLiveEventDisplay
        {
            Bucket = bucket,
            Title = table.Name.ToUpperInvariant(),
            Subtitle = FormatQualifiedTableName(table.Schema, table.Name, captured.Info.DatabaseName),
            Captured = captured,
        };
    }

    private static ProfilerLiveEventDisplay CreateReadOnlyDatabaseEvent(CapturedSqlEvent captured)
    {
        var databaseName = string.IsNullOrWhiteSpace(captured.Info.DatabaseName)
            ? "(unknown)"
            : captured.Info.DatabaseName;

        return new ProfilerLiveEventDisplay
        {
            Bucket = ProfilerEventBucket.ReadOnly,
            Title = databaseName,
            Subtitle = string.Empty,
            Captured = captured,
        };
    }

    private static string FormatQualifiedTableName(string? schema, string tableName, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return string.IsNullOrEmpty(schema)
                ? $"[{tableName}]"
                : $"[{schema}].[{tableName}]";
        }

        if (string.IsNullOrEmpty(schema))
        {
            return $"[{databaseName}].[{tableName}]";
        }

        return $"[{databaseName}].[{schema}].[{tableName}]";
    }

    private static string GetShortObjectName(string objectName)
    {
        var lastDot = objectName.LastIndexOf('.');
        return lastDot >= 0 ? objectName[(lastDot + 1)..] : objectName;
    }
}
