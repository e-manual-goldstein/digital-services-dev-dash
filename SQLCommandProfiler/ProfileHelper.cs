using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SQLCommandProfiler;

public sealed class ProfileHelper : IDisposable
{
    private const int PollIntervalMilliseconds = 200;
    private const string PlaceholderSessionName = "{{SESSION_NAME}}";
    private const string ProfilerApplicationName = "SQLCommandProfiler";
    private const string RpcCompletedEventName = "rpc_completed";

    private bool _disposed;
    private volatile bool _stopRequested;
    private Thread? _traceThread;
    private Exception? _traceException;
    private int _profilerSessionId;
    private int _latestBatch;

    private readonly string _connectionString;
    private readonly string _traceName;
    private readonly string _extendedEventsDefinitionFilePath;
    private readonly EventFilterEngine _eventFilters;
    private readonly bool _printEventDetailsOnCapture;
    private readonly EfSqlInterpretationWhen _efSqlInterpretationWhen;
    private readonly string? _traceReportOutputPath;
    private readonly EfSqlInterpreter _efSqlInterpreter = new();
    private readonly object _commandLookupSync = new();
    private readonly string? _commandLookupFilePath;
    private SqlCommandLookupRegistry? _commandLookupRegistry;
    private readonly SqlProfilerSessionOptions _sessionOptions;
    private readonly Dictionary<int, CapturedSqlEvent[]> _batchedEventsInfo = [];
    private readonly HashSet<string> _seenEventKeys = new(StringComparer.Ordinal);

    public ProfileHelper(IConfiguration configuration)
        : this(configuration, new SqlProfilerSessionOptions())
    {
    }

    public ProfileHelper(IConfiguration configuration, SqlProfilerSessionOptions sessionOptions)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sessionOptions);

        _sessionOptions = sessionOptions;

        _connectionString = ProfilerConnectionResolver.Resolve(configuration, sessionOptions.SqlServerInstance);

        var profiler = configuration.GetSection("Profiler");
        _traceName = profiler["TraceName"]
            ?? throw new InvalidOperationException("Profiler:TraceName is required.");
        _extendedEventsDefinitionFilePath = profiler["XEventsDefinitionFilePath"]
            ?? throw new InvalidOperationException("Profiler:XEventsDefinitionFilePath is required.");

        _eventFilters = new EventFilterEngine(configuration);
        var printFromConfig = bool.TryParse(profiler["PrintEventDetailsOnCapture"], out var printDetails) && printDetails;
        _printEventDetailsOnCapture = !_sessionOptions.SuppressConsoleOutput && printFromConfig;
        _efSqlInterpretationWhen = EfSqlInterpretationOptions.ReadFrom(profiler);
        _traceReportOutputPath = profiler["TraceReportOutputPath"];

        var lookupPath = profiler["SQLCommandLookupFilePath"];
        if (!string.IsNullOrWhiteSpace(lookupPath))
        {
            _commandLookupFilePath = lookupPath;
            _commandLookupRegistry = SqlCommandLookupRegistry.Load(ResolveContentFilePath(lookupPath));
        }
    }

    public string? CommandLookupFilePath => _commandLookupFilePath;

    public bool TryReloadCommandLookup(out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(_commandLookupFilePath))
        {
            errorMessage = "Profiler:SQLCommandLookupFilePath is not configured.";
            return false;
        }

        try
        {
            var registry = SqlCommandLookupRegistry.Load(ResolveContentFilePath(_commandLookupFilePath));
            lock (_commandLookupSync)
            {
                _commandLookupRegistry = registry;
            }

            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public void RefreshCommandLookup(CapturedSqlEvent captured)
    {
        ApplyCommandLookup(captured, forceRefresh: true);
    }

    public static void ValidateExtendedEventsTarget(IConfiguration configuration, string sqlServerInstance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlServerInstance);

        var options = new SqlProfilerSessionOptions
        {
            SqlServerInstance = sqlServerInstance.Trim(),
            ApplicationName = "DigitalDevServices.DevDash-Probe",
            SuppressConsoleOutput = true,
        };

        using var helper = new ProfileHelper(configuration, options);
        helper.ProveExtendedEventsSessionCapability();
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

    public bool TryAddEventFilter(ProfilerEventFilterDefinition definition, out string? errorMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _eventFilters.TryAddRule(definition, out errorMessage);
    }

    public bool TryRemoveEventFilter(int runtimeFilterIndex, out string? errorMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _eventFilters.TryRemoveRuntimeRule(runtimeFilterIndex, out errorMessage);
    }

    public bool PassesEventFilters(in ExtendedEventInfo eventInfo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _eventFilters.PassesFilters(in eventInfo);
    }

    private void ProveExtendedEventsSessionCapability()
    {
        using var connection = CreateProfilerConnection();
        connection.Open();
        InitialiseExtendedEventSession(connection);
        StopSession(connection);
    }

    private void TraceLoop()
    {
        try
        {
            using var connection = CreateProfilerConnection();
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

    private List<CapturedSqlEvent> ReadNewEvents(SqlConnection connection)
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
        var events = new List<CapturedSqlEvent>();

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
                ?? GetEventField(eventElement, "batch_text")
                ?? string.Empty;

            var dedupeKey = $"{timestampUtc.UtcTicks}|{eventName}|{sessionId}|{sqlText}";
            if (!_seenEventKeys.Add(dedupeKey))
            {
                continue;
            }

            var databaseName = GetEventField(eventElement, "database_name") ?? string.Empty;
            var eventInfo = new ExtendedEventInfo(
                TimestampUtc: timestampUtc,
                EventName: eventName,
                ObjectName: GetEventField(eventElement, "object_name") ?? string.Empty,
                DatabaseName: databaseName,
                UserName: GetEventField(eventElement, "username") ?? string.Empty,
                ApplicationName: GetEventField(eventElement, "client_app_name") ?? string.Empty,
                HostName: GetEventField(eventElement, "client_hostname") ?? string.Empty,
                SessionId: sessionId,
                ClientProcessId: ParseInt(GetEventField(eventElement, "client_pid")),
                SqlText: sqlText,
                DurationMicroseconds: ParseLong(GetEventField(eventElement, "duration")),
                CpuMicroseconds: ParseLong(GetEventField(eventElement, "cpu_time")),
                LogicalReads: ParseLong(GetEventField(eventElement, "logical_reads")),
                QueryHash: ParseULong(GetEventField(eventElement, "query_hash")),
                QueryPlanHash: ParseULong(GetEventField(eventElement, "query_plan_hash")));

            if (IsProfilerOwnActivity(eventInfo))
            {
                continue;
            }

            if (!_eventFilters.PassesFilters(eventInfo))
            {
                continue;
            }

            if (BuiltInSqlServerRpc.IsExcludedFromCapture(eventInfo.ObjectName))
            {
                continue;
            }

            var captured = new CapturedSqlEvent { Info = eventInfo };

            if (ShouldInterpretOnReceive())
            {
                ApplyRpcInterpretations(captured);
            }

            events.Add(captured);

            NotifyLiveEvent(captured);

            if (_printEventDetailsOnCapture)
            {
                Console.WriteLine(FormatEventCaptureSummary(eventInfo));
            }
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

    private static void StopSession(SqlConnection connection, string traceName)
    {
        using var command = new SqlCommand(
            $"ALTER EVENT SESSION {QuoteBracketIdentifier(traceName)} ON SERVER STATE = STOP;",
            connection);
        command.ExecuteNonQuery();
    }

    private void StopSession(SqlConnection connection)
    {
        StopSession(connection, _traceName);
    }

    private void StopExtendedEventSession()
    {
        using var connection = CreateProfilerConnection();
        connection.Open();

        if (!SessionExists(connection, _traceName) || !SessionIsRunning(connection, _traceName))
        {
            return;
        }

        StopSession(connection);
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

    public string CreateTraceReport()
    {
        var capturedEvents = _batchedEventsInfo
            .OrderBy(pair => pair.Key)
            .SelectMany(pair => pair.Value)
            .ToList();

        if (_efSqlInterpretationWhen == EfSqlInterpretationWhen.OnReport)
        {
            foreach (var captured in capturedEvents)
            {
                ApplyRpcInterpretations(captured);
            }
        }

        var document = TraceReportBuilder.Build(
            _traceName,
            _batchedEventsInfo.Count,
            capturedEvents,
            _efSqlInterpretationWhen,
            _commandLookupRegistry is not null);

        var outputPath = ResolveTraceReportOutputPath();
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(outputPath, TraceReportJson.Serialize(document));
        return outputPath;
    }

    private string ResolveContentFilePath(string relativeOrAbsolutePath)
    {
        if (Path.IsPathRooted(relativeOrAbsolutePath))
        {
            return relativeOrAbsolutePath;
        }

        return Path.Combine(AppContext.BaseDirectory, relativeOrAbsolutePath);
    }

    private string ResolveTraceReportOutputPath()
    {
        if (string.IsNullOrWhiteSpace(_traceReportOutputPath))
        {
            var fileName = $"SQLCommandProfiler-{_traceName}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
            return Path.Combine(AppContext.BaseDirectory, fileName);
        }

        if (Path.IsPathRooted(_traceReportOutputPath))
        {
            return _traceReportOutputPath;
        }

        return Path.Combine(AppContext.BaseDirectory, _traceReportOutputPath);
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

    private SqlConnection CreateProfilerConnection()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            ApplicationName = _sessionOptions.ApplicationName,
        };
        return new SqlConnection(builder.ConnectionString);
    }

    private bool IsProfilerOwnActivity(in ExtendedEventInfo eventInfo)
    {
        if (string.Equals(eventInfo.ApplicationName, _sessionOptions.ApplicationName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventInfo.ApplicationName, ProfilerApplicationName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(eventInfo.SqlText))
        {
            return false;
        }

        return eventInfo.SqlText.Contains("sys.dm_xe_session_targets", StringComparison.OrdinalIgnoreCase)
            || eventInfo.SqlText.Contains("sys.dm_xe_sessions", StringComparison.OrdinalIgnoreCase)
            || eventInfo.SqlText.Contains("sys.server_event_sessions", StringComparison.OrdinalIgnoreCase)
            || eventInfo.SqlText.Contains("ALTER EVENT SESSION", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetEventField(XElement eventElement, string fieldName)
    {
        foreach (var element in eventElement.Elements())
        {
            if (!string.Equals((string?)element.Attribute("name"), fieldName, StringComparison.Ordinal))
            {
                continue;
            }

            var attributeValue = (string?)element.Attribute("value");
            if (!string.IsNullOrEmpty(attributeValue))
            {
                return attributeValue;
            }

            foreach (var child in element.Elements())
            {
                if (string.Equals(child.Name.LocalName, "value", StringComparison.OrdinalIgnoreCase))
                {
                    return child.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(element.Value))
            {
                return element.Value;
            }
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

    private static ulong ParseULong(string? value)
    {
        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static string FormatEventCaptureSummary(ExtendedEventInfo info)
    {
        var durationMs = info.DurationMicroseconds / 1000d;
        var sqlSnippet = FormatSqlSnippet(info.SqlText);

        return
            $"[capture] {info.TimestampUtc:HH:mm:ss.fff} | {info.EventName} | db={info.DatabaseName} | spid={info.SessionId} | clientPid={info.ClientProcessId} | {durationMs:0.###} ms | {sqlSnippet}";
    }

    private static string FormatSqlSnippet(string sqlText, int maxLength = 96)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            return "(no text)";
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

        if (singleLine.Length <= maxLength)
        {
            return singleLine;
        }

        return string.Concat(singleLine.AsSpan(0, maxLength - 3), "...");
    }

    private void ApplyRpcInterpretations(CapturedSqlEvent captured)
    {
        ApplyEfInterpretation(captured);
        ApplyCommandLookup(captured);
    }

    private void ApplyEfInterpretation(CapturedSqlEvent captured)
    {
        if (!string.Equals(captured.Info.EventName, RpcCompletedEventName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (captured.EfInterpretation is not null)
        {
            return;
        }

        captured.EfInterpretation = _efSqlInterpreter.Interpret(captured.Info.SqlText);
    }

    private void ApplyCommandLookup(CapturedSqlEvent captured, bool forceRefresh = false)
    {
        if (!string.Equals(captured.Info.EventName, RpcCompletedEventName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lock (_commandLookupSync)
        {
            if (_commandLookupRegistry is null)
            {
                captured.CommandLookup = null;
                return;
            }

            if (!forceRefresh && captured.CommandLookup is not null)
            {
                return;
            }

            captured.CommandLookup = _commandLookupRegistry.Match(captured.Info.ObjectName);
        }
    }

    private bool ShouldInterpretOnReceive()
    {
        return _sessionOptions.OnLiveEvent is not null
            || _efSqlInterpretationWhen == EfSqlInterpretationWhen.OnReceive;
    }

    private void NotifyLiveEvent(CapturedSqlEvent captured)
    {
        if (_sessionOptions.OnLiveEvent is null)
        {
            return;
        }

        var display = ProfilerLiveEventClassifier.CreateClassifiedDisplay(captured);

        _sessionOptions.OnLiveEvent(display);
    }
}
