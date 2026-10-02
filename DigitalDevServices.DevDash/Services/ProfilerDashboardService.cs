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

    public List<ProfilerLiveEventDisplay> Inserts { get; } = [];

    public List<ProfilerLiveEventDisplay> Updates { get; } = [];

    public List<ProfilerLiveEventDisplay> ReadOnlyEvents { get; } = [];

    public List<ProfilerLiveEventDisplay> UnknownEvents { get; } = [];

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

    public event Action? Changed;

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

            Inserts.Clear();
            Updates.Clear();
            ReadOnlyEvents.Clear();
            UnknownEvents.Clear();
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
                FilterFeedbackMessage =
                    $"{definition.Effect} filter added on {definition.Field} (pattern: {definition.Pattern}).";
                errorMessage = null;
                success = true;
            }
        }

        NotifyChanged();
        return success;
    }

    private void HandleLiveEvent(ProfilerLiveEventDisplay display)
    {
        lock (_sync)
        {
            switch (display.Bucket)
            {
                case ProfilerEventBucket.Insert:
                    Inserts.Add(display);
                    break;
                case ProfilerEventBucket.Update:
                    Updates.Add(display);
                    break;
                case ProfilerEventBucket.ReadOnly:
                    ReadOnlyEvents.Add(display);
                    break;
                case ProfilerEventBucket.Unknown:
                    UnknownEvents.Add(display);
                    break;
            }
        }

        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();
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
