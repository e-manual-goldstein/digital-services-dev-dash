using System.Globalization;
using System.Text;
using SQLCommandProfiler;

namespace DigitalDevServices.DevDash.Services;

public sealed class ProfilerDashboardService
{
    private readonly IConfiguration _configuration;
    private readonly object _sync = new();
    private ProfileHelper? _session;

    public ProfilerDashboardService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsRunning { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? LastReportPath { get; private set; }

    public List<ProfilerLiveEventDisplay> Inserts { get; } = [];

    public List<ProfilerLiveEventDisplay> Updates { get; } = [];

    public List<ProfilerLiveEventDisplay> ReadOnlyEvents { get; } = [];

    public event Action? Changed;

    public void BeginProfiling()
    {
        lock (_sync)
        {
            if (IsRunning)
            {
                return;
            }

            Inserts.Clear();
            Updates.Clear();
            ReadOnlyEvents.Clear();
            ErrorMessage = null;
            LastReportPath = null;

            var options = new SqlProfilerSessionOptions
            {
                ApplicationName = "DigitalDevServices.DevDash",
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
        builder.AppendLine(CultureInfo.InvariantCulture, $"Session id: {info.SessionId}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Object: {info.ObjectName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Duration (ms): {info.DurationMicroseconds / 1000d:0.###}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Logical reads: {info.LogicalReads}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Query hash: {info.QueryHash:X16}");
        builder.AppendLine();
        builder.AppendLine("SQL:");
        builder.AppendLine(info.SqlText);
        return builder.ToString();
    }
}
