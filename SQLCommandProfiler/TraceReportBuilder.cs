namespace SQLCommandProfiler;

internal static class TraceReportBuilder
{
    private const int SqlSnippetMaxLength = 160;

    public static TraceReportDocument Build(
        string traceSessionName,
        int batchCount,
        IReadOnlyList<CapturedSqlEvent> capturedEvents,
        EfSqlInterpretationWhen interpretationWhen)
    {
        var summary = BuildSummary(batchCount, capturedEvents);
        var efReport = BuildEfCommandReport(capturedEvents, interpretationWhen);
        var byApplication = BuildApplicationGroups(capturedEvents);

        return new TraceReportDocument
        {
            TraceSessionName = traceSessionName,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Summary = summary,
            EfCommandReport = efReport,
            CommandsByApplicationName = byApplication,
        };
    }

    private static TraceReportSummary BuildSummary(int batchCount, IReadOnlyList<CapturedSqlEvent> capturedEvents)
    {
        var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byDatabase = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var captured in capturedEvents)
        {
            Increment(byType, captured.Info.EventName);
            Increment(byDatabase, string.IsNullOrWhiteSpace(captured.Info.DatabaseName) ? "(unknown)" : captured.Info.DatabaseName);
        }

        return new TraceReportSummary
        {
            TotalEventsCaptured = capturedEvents.Count,
            BatchCount = batchCount,
            EventsByType = byType,
            EventsByDatabase = byDatabase,
        };
    }

    private static EfCommandReport BuildEfCommandReport(
        IReadOnlyList<CapturedSqlEvent> capturedEvents,
        EfSqlInterpretationWhen interpretationWhen)
    {
        var interpretationEnabled = interpretationWhen != EfSqlInterpretationWhen.Never;
        var createsByTable = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var updatesByTable = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var ef6Commands = 0;
        var entitiesCreated = 0;
        var entitiesUpdated = 0;
        var entitiesDeleted = 0;
        var readOnlyCommands = 0;
        var unclassified = 0;

        foreach (var captured in capturedEvents)
        {
            if (captured.EfInterpretation is null || !captured.EfInterpretation.IsLikelyEf6)
            {
                unclassified++;
                continue;
            }

            ef6Commands++;

            if (captured.EfInterpretation.Access == EfSqlAccess.ReadOnly)
            {
                readOnlyCommands++;
            }

            foreach (var statement in captured.EfInterpretation.Statements)
            {
                switch (statement.Kind)
                {
                    case EfSqlStatementKind.Insert when statement.Insert is not null:
                        entitiesCreated++;
                        Increment(createsByTable, FormatTable(statement.Insert.Table));
                        break;
                    case EfSqlStatementKind.Update when statement.Update is not null:
                        entitiesUpdated++;
                        Increment(updatesByTable, FormatTable(statement.Update.Table));
                        break;
                    case EfSqlStatementKind.Delete:
                        entitiesDeleted++;
                        break;
                }
            }
        }

        return new EfCommandReport
        {
            InterpretationEnabled = interpretationEnabled,
            Ef6CommandsIdentified = ef6Commands,
            EntitiesCreated = entitiesCreated,
            EntitiesUpdated = entitiesUpdated,
            EntitiesDeleted = entitiesDeleted,
            ReadOnlyCommands = readOnlyCommands,
            UnclassifiedOrNonEfCommands = unclassified,
            CreatesByTable = createsByTable,
            UpdatesByTable = updatesByTable,
        };
    }

    private static IReadOnlyList<ApplicationCommandGroup> BuildApplicationGroups(IReadOnlyList<CapturedSqlEvent> capturedEvents)
    {
        return capturedEvents
            .GroupBy(captured => string.IsNullOrWhiteSpace(captured.Info.ApplicationName)
                ? "(unknown)"
                : captured.Info.ApplicationName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var captured in group)
                {
                    Increment(byType, captured.Info.EventName);
                }

                var events = group
                    .OrderBy(captured => captured.Info.TimestampUtc)
                    .Select(ToEventSummary)
                    .ToArray();

                return new ApplicationCommandGroup
                {
                    ApplicationName = group.Key,
                    EventCount = group.Count(),
                    EventsByType = byType,
                    Events = events,
                };
            })
            .ToArray();
    }

    private static CapturedEventSummary ToEventSummary(CapturedSqlEvent captured)
    {
        return new CapturedEventSummary
        {
            TimestampUtc = captured.Info.TimestampUtc,
            EventName = captured.Info.EventName,
            DatabaseName = captured.Info.DatabaseName,
            UserName = captured.Info.UserName,
            HostName = captured.Info.HostName,
            DurationMicroseconds = captured.Info.DurationMicroseconds,
            SqlSnippet = FormatSqlSnippet(captured.Info.SqlText),
            EfAccess = captured.EfInterpretation?.Access,
        };
    }

    private static string? FormatSqlSnippet(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            return null;
        }

        var singleLine = sqlText
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();

        while (singleLine.Contains("  ", StringComparison.Ordinal))
        {
            singleLine = singleLine.Replace("  ", " ", StringComparison.Ordinal);
        }

        if (singleLine.Length <= SqlSnippetMaxLength)
        {
            return singleLine;
        }

        return string.Concat(singleLine.AsSpan(0, SqlSnippetMaxLength - 3), "...");
    }

    private static string FormatTable(EfTableReference table)
    {
        return string.IsNullOrEmpty(table.Schema)
            ? table.Name
            : $"{table.Schema}.{table.Name}";
    }

    private static void Increment(IDictionary<string, int> counts, string key)
    {
        counts.TryGetValue(key, out var current);
        counts[key] = current + 1;
    }
}
