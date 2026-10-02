using System.Globalization;
using System.Text;
using SQLCommandProfiler;

namespace DigitalDevServices.DevDash.Services;

public sealed class ProfilerDashboardService
{
    private readonly IConfiguration _configuration;
    private readonly object _sync = new();
    private ProfileHelper? _session;
    private string? _activeSqlServerInstance;
    private bool _canBeginProfiling = true;

    public ProfilerDashboardService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsRunning { get; private set; }

    public bool IsCheckingTarget { get; private set; }

    public bool CanBeginProfiling
    {
        get
        {
            lock (_sync)
            {
                return !IsRunning && !IsCheckingTarget && _canBeginProfiling;
            }
        }
    }

    public string? ErrorMessage { get; private set; }

    public string? TargetWarningMessage { get; private set; }

    public string? TargetReadyMessage { get; private set; }

    public string? LastReportPath { get; private set; }

    public string ActiveTargetDescription
    {
        get
        {
            lock (_sync)
            {
                return string.IsNullOrWhiteSpace(_activeSqlServerInstance)
                    ? "Default (ConnectionStrings:Default)"
                    : _activeSqlServerInstance;
            }
        }
    }

    private readonly List<ProfilerLiveEventDisplay> _inserts = [];
    private readonly List<ProfilerLiveEventDisplay> _updates = [];
    private readonly List<ProfilerLiveEventDisplay> _deletes = [];
    private readonly List<ProfilerLiveEventDisplay> _readOnlyEvents = [];
    private readonly List<ProfilerLiveEventDisplay> _recognisedCommands = [];
    private readonly List<ProfilerLiveEventDisplay> _unknownEvents = [];

    private ProfilerLiveEventDisplay[] _insertsSnapshot = [];
    private ProfilerLiveEventDisplay[] _updatesSnapshot = [];
    private ProfilerLiveEventDisplay[] _deletesSnapshot = [];
    private ProfilerLiveEventDisplay[] _readOnlyEventsSnapshot = [];
    private ProfilerLiveEventDisplay[] _recognisedCommandsSnapshot = [];
    private ProfilerLiveEventDisplay[] _unknownEventsSnapshot = [];

    public IReadOnlyList<ProfilerLiveEventDisplay> Inserts => _insertsSnapshot;

    public IReadOnlyList<ProfilerLiveEventDisplay> Updates => _updatesSnapshot;

    public IReadOnlyList<ProfilerLiveEventDisplay> Deletes => _deletesSnapshot;

    public IReadOnlyList<ProfilerLiveEventDisplay> ReadOnlyEvents => _readOnlyEventsSnapshot;

    public IReadOnlyList<ProfilerLiveEventDisplay> RecognisedCommands => _recognisedCommandsSnapshot;

    public IReadOnlyList<ProfilerLiveEventDisplay> UnknownEvents => _unknownEventsSnapshot;

    public IReadOnlyList<ProfilerEventFilterDefinition> RuntimeFilters
    {
        get
        {
            lock (_sync)
            {
                return _runtimeFilters.ToArray();
            }
        }
    }

    public string? FilterFeedbackMessage { get; private set; }

    public string CommandLookupConfiguredPath =>
        _configuration["Profiler:SQLCommandLookupFilePath"] ?? string.Empty;

    public event Action? Changed;

    private readonly List<ProfilerLiveEventDisplay> _masterDisplays = [];
    private readonly List<ProfilerEventFilterDefinition> _runtimeFilters = [];

    public async Task<bool> ApplySqlServerTargetAsync(string sqlServerInstanceInput)
    {
        if (IsRunning)
        {
            return false;
        }

        var trimmed = sqlServerInstanceInput.Trim();

        lock (_sync)
        {
            IsCheckingTarget = true;
            _canBeginProfiling = false;
            TargetWarningMessage = null;
            TargetReadyMessage = null;
        }

        NotifyChanged();

        try
        {
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                lock (_sync)
                {
                    _activeSqlServerInstance = null;
                    _canBeginProfiling = true;
                    TargetReadyMessage = "Using the default SQL Server from configuration. Click Begin profiling when ready.";
                }

                return true;
            }

            await Task.Run(() => ProfileHelper.ValidateExtendedEventsTarget(_configuration, trimmed))
                .ConfigureAwait(false);

            lock (_sync)
            {
                _activeSqlServerInstance = trimmed;
                _canBeginProfiling = true;
                TargetReadyMessage =
                    $"Extended Events session verified on \"{trimmed}\". Click Begin profiling when ready.";
            }

            return true;
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _activeSqlServerInstance = null;
                _canBeginProfiling = false;
                TargetWarningMessage = ex.InnerException?.Message ?? ex.Message;
            }

            return false;
        }
        finally
        {
            lock (_sync)
            {
                IsCheckingTarget = false;
            }

            NotifyChanged();
        }
    }

    public void BeginProfiling()
    {
        lock (_sync)
        {
            if (IsRunning || !_canBeginProfiling)
            {
                return;
            }

            ClearAllBuckets();
            _masterDisplays.Clear();
            _runtimeFilters.Clear();
            FilterFeedbackMessage = null;
            ErrorMessage = null;
            LastReportPath = null;

            var options = new SqlProfilerSessionOptions
            {
                ApplicationName = "DigitalDevServices.DevDash",
                SqlServerInstance = _activeSqlServerInstance,
                SuppressConsoleOutput = true,
                OnLiveEvent = HandleLiveEvent,
            };

            try
            {
                _session = new ProfileHelper(_configuration, options);
                _session.BeginTrace();
                IsRunning = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _session?.Dispose();
                _session = null;
                IsRunning = false;
            }
        }

        NotifyChanged();
    }

    public void StopProfiling()
    {
        ProfileHelper? session;
        lock (_sync)
        {
            session = _session;
            _session = null;
            IsRunning = false;
        }

        if (session is null)
        {
            NotifyChanged();
            return;
        }

        try
        {
            session.EndTrace();
            LastReportPath = session.CreateTraceReport();
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                ErrorMessage = ex.Message;
            }
        }
        finally
        {
            session.Dispose();
            NotifyChanged();
        }
    }

    public bool TryAddRuntimeFilter(ProfilerEventFilterDefinition definition, out string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(definition);

        bool success;
        lock (_sync)
        {
            if (!IsRunning || _session is null)
            {
                errorMessage = "Start profiling before adding filters.";
                FilterFeedbackMessage = null;
                success = false;
            }
            else if (!_session.TryAddEventFilter(definition, out errorMessage))
            {
                FilterFeedbackMessage = null;
                success = false;
            }
            else
            {
                _runtimeFilters.Add(definition);
                RebuildVisibleBuckets();
                FilterFeedbackMessage =
                    $"{definition.Effect} filter added on {definition.Field} (pattern: {definition.Pattern}).";
                errorMessage = null;
                success = true;
            }
        }

        NotifyChanged();
        return success;
    }

    public bool TryRemoveRuntimeFilter(int runtimeFilterIndex, out string? errorMessage)
    {
        bool success;
        lock (_sync)
        {
            if (!IsRunning || _session is null)
            {
                errorMessage = "Start profiling before removing filters.";
                FilterFeedbackMessage = null;
                success = false;
            }
            else if (runtimeFilterIndex < 0 || runtimeFilterIndex >= _runtimeFilters.Count)
            {
                errorMessage = "Runtime filter was not found.";
                FilterFeedbackMessage = null;
                success = false;
            }
            else if (!_session.TryRemoveEventFilter(runtimeFilterIndex, out errorMessage))
            {
                FilterFeedbackMessage = null;
                success = false;
            }
            else
            {
                var removed = _runtimeFilters[runtimeFilterIndex];
                _runtimeFilters.RemoveAt(runtimeFilterIndex);
                RebuildVisibleBuckets();
                FilterFeedbackMessage =
                    $"{removed.Effect} filter removed from {removed.Field} (pattern: {removed.Pattern}).";
                errorMessage = null;
                success = true;
            }
        }

        NotifyChanged();
        return success;
    }

    public bool TryReloadRecognisedCommandsLookup(out string? errorMessage)
    {
        lock (_sync)
        {
            if (!IsRunning || _session is null)
            {
                errorMessage = "Start profiling before reloading the command lookup.";
                FilterFeedbackMessage = null;
                NotifyChanged();
                return false;
            }

            if (!_session.TryReloadCommandLookup(out errorMessage))
            {
                FilterFeedbackMessage = null;
                NotifyChanged();
                return false;
            }

            ReclassifyMasterDisplays();
            RebuildVisibleBuckets();
            FilterFeedbackMessage =
                $"Command lookup reloaded from {_session.CommandLookupFilePath ?? CommandLookupConfiguredPath}.";
            errorMessage = null;
            NotifyChanged();
            return true;
        }
    }

    public void ClearBucket(ProfilerEventBucket bucket)
    {
        lock (_sync)
        {
            _masterDisplays.RemoveAll(display => display.Bucket == bucket);
            RebuildVisibleBuckets();
        }

        NotifyChanged();
    }

    public int MasterEventCount
    {
        get
        {
            lock (_sync)
            {
                return _masterDisplays.Count;
            }
        }
    }

    public IReadOnlyList<ProfilerGroupSummaryRow> GetGroupSummary(ProfilerGroupSummaryDimension dimension)
    {
        lock (_sync)
        {
            var accumulators = new Dictionary<string, BucketCounts>(StringComparer.OrdinalIgnoreCase);

            foreach (var display in _masterDisplays)
            {
                var groupKey = ResolveGroupKey(display.Captured.Info, dimension);
                if (!accumulators.TryGetValue(groupKey, out var counts))
                {
                    counts = new BucketCounts();
                    accumulators[groupKey] = counts;
                }

                counts.Increment(display.Bucket);
            }

            return accumulators
                .Select(pair => pair.Value.ToRow(pair.Key))
                .OrderBy(row => row.GroupKey, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    private static string ResolveGroupKey(ExtendedEventInfo info, ProfilerGroupSummaryDimension dimension)
    {
        var raw = dimension switch
        {
            ProfilerGroupSummaryDimension.HostName => info.HostName,
            ProfilerGroupSummaryDimension.ApplicationName => info.ApplicationName,
            ProfilerGroupSummaryDimension.Database => info.DatabaseName,
            ProfilerGroupSummaryDimension.User => info.UserName,
            _ => string.Empty,
        };

        return string.IsNullOrWhiteSpace(raw) ? "(empty)" : raw.Trim();
    }

    private sealed class BucketCounts
    {
        public int Inserts;
        public int Updates;
        public int Deletes;
        public int ReadOnly;
        public int Recognised;
        public int Unknown;

        public void Increment(ProfilerEventBucket bucket)
        {
            switch (bucket)
            {
                case ProfilerEventBucket.Insert:
                    Inserts++;
                    break;
                case ProfilerEventBucket.Update:
                    Updates++;
                    break;
                case ProfilerEventBucket.Delete:
                    Deletes++;
                    break;
                case ProfilerEventBucket.ReadOnly:
                    ReadOnly++;
                    break;
                case ProfilerEventBucket.RecognisedCommand:
                    Recognised++;
                    break;
                case ProfilerEventBucket.Unknown:
                    Unknown++;
                    break;
            }
        }

        public ProfilerGroupSummaryRow ToRow(string groupKey) =>
            new()
            {
                GroupKey = groupKey,
                InsertCount = Inserts,
                UpdateCount = Updates,
                DeleteCount = Deletes,
                ReadOnlyCount = ReadOnly,
                RecognisedCount = Recognised,
                UnknownCount = Unknown,
            };
    }

    private void HandleLiveEvent(ProfilerLiveEventDisplay display)
    {
        lock (_sync)
        {
            _masterDisplays.Add(display);
            if (_session is not null && _session.PassesEventFilters(display.Captured.Info))
            {
                AddToBucket(display);
            }
        }

        NotifyChanged();
    }

    private void ReclassifyMasterDisplays()
    {
        for (var i = 0; i < _masterDisplays.Count; i++)
        {
            var captured = _masterDisplays[i].Captured;
            _session!.RefreshCommandLookup(captured);
            _masterDisplays[i] = ProfilerLiveEventClassifier.CreateClassifiedDisplay(captured);
        }
    }

    private void RebuildVisibleBuckets()
    {
        ClearAllBuckets();
        if (_session is null)
        {
            return;
        }

        foreach (var display in _masterDisplays)
        {
            if (_session.PassesEventFilters(display.Captured.Info))
            {
                AddToBucket(display);
            }
        }
    }

    private void ClearAllBuckets()
    {
        _inserts.Clear();
        _updates.Clear();
        _deletes.Clear();
        _readOnlyEvents.Clear();
        _recognisedCommands.Clear();
        _unknownEvents.Clear();
    }

    private void AddToBucket(ProfilerLiveEventDisplay display)
    {
        switch (display.Bucket)
        {
            case ProfilerEventBucket.Insert:
                _inserts.Add(display);
                break;
            case ProfilerEventBucket.Update:
                _updates.Add(display);
                break;
            case ProfilerEventBucket.Delete:
                _deletes.Add(display);
                break;
            case ProfilerEventBucket.ReadOnly:
                _readOnlyEvents.Add(display);
                break;
            case ProfilerEventBucket.RecognisedCommand:
                _recognisedCommands.Add(display);
                break;
            case ProfilerEventBucket.Unknown:
                _unknownEvents.Add(display);
                break;
        }
    }

    private void PublishStreamSnapshots()
    {
        _insertsSnapshot = _inserts.ToArray();
        _updatesSnapshot = _updates.ToArray();
        _deletesSnapshot = _deletes.ToArray();
        _readOnlyEventsSnapshot = _readOnlyEvents.ToArray();
        _recognisedCommandsSnapshot = _recognisedCommands.ToArray();
        _unknownEventsSnapshot = _unknownEvents.ToArray();
    }

    private void NotifyChanged()
    {
        lock (_sync)
        {
            PublishStreamSnapshots();
        }

        Changed?.Invoke();
    }
}

public static class ProfilerEventDetailFormatter
{
    public static string Format(ProfilerLiveEventDisplay display)
    {
        var info = display.Captured.Info;
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Bucket: {display.Bucket}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Title: {display.Title}");
        if (!string.IsNullOrWhiteSpace(display.Subtitle))
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"Subtitle: {display.Subtitle}");
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"Timestamp (UTC): {info.TimestampUtc:u}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Event: {info.EventName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Database: {info.DatabaseName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Application: {info.ApplicationName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"User: {info.UserName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Host: {info.HostName}");
        if (info.ClientProcessId > 0)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"Client process id: {info.ClientProcessId}");
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"Session id: {info.SessionId}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Object: {info.ObjectName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Duration (ms): {info.DurationMicroseconds / 1000d:0.###}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Logical reads: {info.LogicalReads}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Query hash: {info.QueryHash:X16}");
        builder.AppendLine();
        builder.AppendLine("SQL:");
        builder.AppendLine(SqlTextPrettyPrinter.FormatOrOriginal(info.SqlText));
        return builder.ToString();
    }
}
