using System.Globalization;

namespace SQLCommandProfiler;

internal static class TraceReportBuilder
{
    private const int SqlSnippetMaxLength = 160;

    public static TraceReportDocument Build(
        string traceSessionName,
        int batchCount,
        IReadOnlyList<CapturedSqlEvent> capturedEvents,
        EfSqlInterpretationWhen interpretationWhen,
        bool sqlCommandLookupConfigured)
    {
        var summary = BuildSummary(batchCount, capturedEvents);
        var efReport = BuildEfCommandReport(capturedEvents, interpretationWhen);
        var knownCommands = BuildKnownSqlCommandReport(capturedEvents, sqlCommandLookupConfigured);
        var duplicateCommands = BuildDuplicateCommandsReport(capturedEvents);
        var byApplication = BuildApplicationGroups(capturedEvents);

        return new TraceReportDocument
        {
            TraceSessionName = traceSessionName,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Summary = summary,
            EfCommandReport = efReport,
            KnownSqlCommandReport = knownCommands,
            DuplicateCommands = duplicateCommands,
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

    private static KnownSqlCommandReport BuildKnownSqlCommandReport(
        IReadOnlyList<CapturedSqlEvent> capturedEvents,
        bool sqlCommandLookupConfigured)
    {
        var invocations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rpcWithObjectName = 0;
        var knownReadOnly = 0;
        var knownReadWrite = 0;
        var unknown = 0;

        foreach (var captured in capturedEvents)
        {
            if (!string.Equals(captured.Info.EventName, "rpc_completed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(captured.Info.ObjectName))
            {
                continue;
            }

            rpcWithObjectName++;

            if (captured.CommandLookup is null || !captured.CommandLookup.IsKnown)
            {
                unknown++;
                Increment(invocations, captured.Info.ObjectName);
                continue;
            }

            Increment(invocations, captured.Info.ObjectName);
            if (captured.CommandLookup.Access == SqlCommandAccess.ReadOnly)
            {
                knownReadOnly++;
            }
            else
            {
                knownReadWrite++;
            }
        }

        return new KnownSqlCommandReport
        {
            LookupConfigured = sqlCommandLookupConfigured,
            RpcEventsWithObjectName = rpcWithObjectName,
            KnownReadOnly = knownReadOnly,
            KnownReadWrite = knownReadWrite,
            UnknownCommands = unknown,
            InvocationsByCommand = invocations,
        };
    }

    private static DuplicateCommandsReport BuildDuplicateCommandsReport(IReadOnlyList<CapturedSqlEvent> capturedEvents)
    {
        var byHash = new Dictionary<ulong, List<CapturedSqlEvent>>();

        foreach (var captured in capturedEvents)
        {
            if (captured.Info.QueryHash == 0)
            {
                continue;
            }

            if (!byHash.TryGetValue(captured.Info.QueryHash, out var list))
            {
                list = [];
                byHash[captured.Info.QueryHash] = list;
            }

            list.Add(captured);
        }

        var eventsWithHash = byHash.Values.Sum(group => group.Count);
        var duplicateGroups = byHash
            .Where(pair => pair.Value.Count > 1)
            .OrderByDescending(pair => pair.Value.Count)
            .ThenBy(pair => pair.Key, Comparer<ulong>.Default)
            .Select(pair =>
            {
                var sample = pair.Value[0].Info;
                return new DuplicateCommandGroup
                {
                    QueryHash = pair.Key.ToString("X16", CultureInfo.InvariantCulture),
                    InvocationCount = pair.Value.Count,
                    SampleObjectName = string.IsNullOrWhiteSpace(sample.ObjectName) ? null : sample.ObjectName,
                    SampleApplicationName = string.IsNullOrWhiteSpace(sample.ApplicationName) ? null : sample.ApplicationName,
                    SampleSqlSnippet = FormatSqlSnippet(sample.SqlText),
                };
            })
            .ToArray();

        var redundant = duplicateGroups.Sum(group => group.InvocationCount - 1);

        return new DuplicateCommandsReport
        {
            EventsWithQueryHash = eventsWithHash,
            UniqueQueryHashes = byHash.Count,
            DuplicateQueryHashGroups = duplicateGroups.Length,
            RedundantInvocations = redundant,
            Groups = duplicateGroups,
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
            ObjectName = string.IsNullOrWhiteSpace(captured.Info.ObjectName) ? null : captured.Info.ObjectName,
            DatabaseName = captured.Info.DatabaseName,
            UserName = captured.Info.UserName,
            HostName = captured.Info.HostName,
            DurationMicroseconds = captured.Info.DurationMicroseconds,
            SqlSnippet = FormatSqlSnippet(captured.Info.SqlText),
            EfAccess = captured.EfInterpretation?.Access,
            KnownCommandAccess = captured.CommandLookup?.IsKnown == true ? captured.CommandLookup.Access : null,
            IsKnownCommand = captured.CommandLookup?.IsKnown,
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
