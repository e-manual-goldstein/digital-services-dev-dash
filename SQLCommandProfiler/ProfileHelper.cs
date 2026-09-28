using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

internal sealed class ProfileHelper : IDisposable
{
    private const int PollIntervalMilliseconds = 200;
    private const string PlaceholderSessionName = "{{SESSION_NAME}}";

    private bool _disposed;
    private volatile bool _stopRequested;
    private Thread? _traceThread;
    private Exception? _traceException;
    private int _profilerSessionId;
    private int _latestBatch;

    private readonly string _connectionString;
    private readonly string _traceName;
    private readonly string _extendedEventsDefinitionFilePath;
    private readonly Dictionary<int, ExtendedEventInfo[]> _batchedEventsInfo = [];
    private readonly HashSet<string> _seenEventKeys = new(StringComparer.Ordinal);

    public readonly record struct ExtendedEventInfo(
        DateTimeOffset TimestampUtc,
        string EventName,
        string DatabaseName,
        string UserName,
        string ApplicationName,
        int SessionId,
        string SqlText,
        long DurationMicroseconds,
        long CpuMicroseconds,
        long LogicalReads);

    public ProfileHelper(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        var profiler = configuration.GetSection("Profiler");
        _traceName = profiler["TraceName"]
            ?? throw new InvalidOperationException("Profiler:TraceName is required.");
        _extendedEventsDefinitionFilePath = profiler["XEventsDefinitionFilePath"]
            ?? throw new InvalidOperationException("Profiler:XEventsDefinitionFilePath is required.");
    }

    public void BeginTrace()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_traceThread is not null)
        {
            throw new InvalidOperationException("Trace is already running.");
        }

        _stopRequested = false;
        _traceException = null;
        _seenEventKeys.Clear();
        _batchedEventsInfo.Clear();
        _latestBatch = 0;

        _traceThread = new Thread(TraceLoop)
        {
            Name = "SQLCommandProfiler trace",
            IsBackground = false,
        };
        _traceThread.Start();
    }

    private void TraceLoop()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            _profilerSessionId = GetSessionId(connection);
            InitialiseExtendedEventSession(connection);

            while (GetEvents(connection))
            {
            }
        }
        catch (Exception ex)
        {
            _traceException = ex;
        }
        finally
        {
            try
            {
                StopExtendedEventSession();
            }
            catch (Exception ex) when (_traceException is null)
            {
                _traceException = ex;
            }
        }
    }

    private bool GetEvents(SqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (_stopRequested)
        {
            return false;
        }

        var newEvents = ReadNewEvents(connection);
        if (newEvents.Count > 0)
        {
            var batch = Interlocked.Increment(ref _latestBatch);
            _batchedEventsInfo[batch] = newEvents.ToArray();
        }

        Thread.Sleep(PollIntervalMilliseconds);
        return !_stopRequested;
    }

    private List<ExtendedEventInfo> ReadNewEvents(SqlConnection connection)
    {
        const string sql = """
            SELECT CAST(t.target_data AS XML) AS TargetData
            FROM sys.dm_xe_session_targets AS t
            INNER JOIN sys.dm_xe_sessions AS s ON s.address = t.event_session_address
            WHERE s.name = @SessionName AND t.target_name = N'ring_buffer';
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SessionName", _traceName);

        var result = command.ExecuteScalar();
        if (result is DBNull or null)
        {
            return [];
        }

        var targetXml = XDocument.Parse(result.ToString()!);
        var events = new List<ExtendedEventInfo>();

        foreach (var eventElement in targetXml.Descendants("event"))
        {
            var timestampRaw = (string?)eventElement.Attribute("timestamp");
            var eventName = (string?)eventElement.Attribute("name") ?? string.Empty;
            if (string.IsNullOrEmpty(timestampRaw)
                || !DateTimeOffset.TryParse(
                    timestampRaw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var timestampUtc))
            {
                continue;
            }

            timestampUtc = timestampUtc.ToUniversalTime();

            var sessionId = ParseInt(GetEventField(eventElement, "session_id"));
            if (sessionId == _profilerSessionId)
            {
                continue;
            }

            var sqlText = GetEventField(eventElement, "sql_text")
                ?? GetEventField(eventElement, "statement")
                ?? string.Empty;

            var dedupeKey = $"{timestampUtc.UtcTicks}|{eventName}|{sessionId}|{sqlText}";
            if (!_seenEventKeys.Add(dedupeKey))
            {
                continue;
            }

            events.Add(new ExtendedEventInfo(
                TimestampUtc: timestampUtc,
                EventName: eventName,
                DatabaseName: GetEventField(eventElement, "database_name") ?? string.Empty,
                UserName: GetEventField(eventElement, "username") ?? string.Empty,
                ApplicationName: GetEventField(eventElement, "client_app_name") ?? string.Empty,
                SessionId: sessionId,
                SqlText: sqlText,
                DurationMicroseconds: ParseLong(GetEventField(eventElement, "duration")),
                CpuMicroseconds: ParseLong(GetEventField(eventElement, "cpu_time")),
                LogicalReads: ParseLong(GetEventField(eventElement, "logical_reads"))));
        }

        return events;
    }

    private void InitialiseExtendedEventSession(SqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (!SessionExists(connection, _traceName))
        {
            CreateSession(connection);
        }

        if (!SessionIsRunning(connection, _traceName))
        {
            StartSession(connection);
        }
    }

    private void CreateSession(SqlConnection connection)
    {
        var definitionPath = ResolveDefinitionFilePath();
        if (!File.Exists(definitionPath))
        {
            throw new FileNotFoundException("Extended Events definition file was not found.", definitionPath);
        }

        var definition = File.ReadAllText(definitionPath);
        if (!definition.Contains(PlaceholderSessionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Extended Events definition must contain the {PlaceholderSessionName} placeholder.");
        }

        definition = definition.Replace(PlaceholderSessionName, EscapeSessionNameForCreate(_traceName), StringComparison.Ordinal);

        foreach (var batch in SplitSqlBatches(definition))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            using var command = new SqlCommand(batch, connection);
            command.ExecuteNonQuery();
        }
    }

    private void StartSession(SqlConnection connection)
    {
        using var command = new SqlCommand(
            $"ALTER EVENT SESSION {QuoteBracketIdentifier(_traceName)} ON SERVER STATE = START;",
            connection);
        command.ExecuteNonQuery();
    }

    private void StopExtendedEventSession()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        if (!SessionExists(connection, _traceName) || !SessionIsRunning(connection, _traceName))
        {
            return;
        }

        using var command = new SqlCommand(
            $"ALTER EVENT SESSION {QuoteBracketIdentifier(_traceName)} ON SERVER STATE = STOP;",
            connection);
        command.ExecuteNonQuery();
    }

    public void EndTrace()
    {
        _stopRequested = true;

        if (_traceThread is null)
        {
            return;
        }

        _traceThread.Join();
        _traceThread = null;

        if (_traceException is not null)
        {
            throw new InvalidOperationException("The trace thread failed.", _traceException);
        }
    }

    public void PrintTraceReport()
    {
        if (_batchedEventsInfo.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("No SQL commands were captured during this trace.");
            return;
        }

        var totalEvents = _batchedEventsInfo.Values.Sum(batch => batch.Length);
        Console.WriteLine();
        Console.WriteLine($"Trace report — session \"{_traceName}\" — {totalEvents} event(s) in {_batchedEventsInfo.Count} batch(es).");
        Console.WriteLine(new string('-', 80));

        foreach (var (batchNumber, batch) in _batchedEventsInfo.OrderBy(pair => pair.Key))
        {
            Console.WriteLine();
            Console.WriteLine($"Batch {batchNumber} ({batch.Length} event(s))");

            foreach (var info in batch)
            {
                WriteEvent(info);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            EndTrace();
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            if (ex.InnerException is not null)
            {
                Console.Error.WriteLine(ex.InnerException);
            }
        }

        _disposed = true;
    }

    private string ResolveDefinitionFilePath()
    {
        if (Path.IsPathRooted(_extendedEventsDefinitionFilePath))
        {
            return _extendedEventsDefinitionFilePath;
        }

        return Path.Combine(AppContext.BaseDirectory, _extendedEventsDefinitionFilePath);
    }

    private static int GetSessionId(SqlConnection connection)
    {
        using var command = new SqlCommand("SELECT @@SPID;", connection);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static bool SessionExists(SqlConnection connection, string sessionName)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM sys.server_event_sessions
            WHERE name = @SessionName;
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SessionName", sessionName);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static bool SessionIsRunning(SqlConnection connection, string sessionName)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM sys.dm_xe_sessions
            WHERE name = @SessionName;
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SessionName", sessionName);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        var batch = new StringBuilder();
        using var reader = new StringReader(script);

        while (reader.ReadLine() is { } line)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (batch.Length > 0)
                {
                    yield return batch.ToString();
                    batch.Clear();
                }

                continue;
            }

            batch.AppendLine(line);
        }

        if (batch.Length > 0)
        {
            yield return batch.ToString();
        }
    }

    private static string EscapeSessionNameForCreate(string sessionName)
    {
        return sessionName.Replace("]", "]]", StringComparison.Ordinal);
    }

    private static string QuoteBracketIdentifier(string identifier)
    {
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    private static string? GetEventField(XElement eventElement, string fieldName)
    {
        foreach (var element in eventElement.Elements())
        {
            if (!string.Equals((string?)element.Attribute("name"), fieldName, StringComparison.Ordinal))
            {
                continue;
            }

            return (string?)element.Attribute("value");
        }

        return null;
    }

    private static int ParseInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static long ParseLong(string? value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static void WriteEvent(ExtendedEventInfo info)
    {
        var durationMs = info.DurationMicroseconds / 1000d;
        Console.WriteLine(
            $"[{info.TimestampUtc:u}] {info.EventName} | db={info.DatabaseName} | spid={info.SessionId} | user={info.UserName} | app={info.ApplicationName} | {durationMs:0.###} ms | reads={info.LogicalReads}");

        if (string.IsNullOrWhiteSpace(info.SqlText))
        {
            return;
        }

        foreach (var line in info.SqlText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            Console.WriteLine($"    {line}");
        }
    }
}
