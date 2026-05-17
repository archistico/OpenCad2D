using System;
using System.Diagnostics;

namespace OpenCad2D.App.Diagnostics;

public sealed class TraceApplicationLogger : IApplicationLogger
{
    public static TraceApplicationLogger Instance { get; } = new();

    private TraceApplicationLogger()
    {
    }

    public void Info(string category, string message)
    {
        Write(new ApplicationLogEntry(
            DateTimeOffset.Now,
            ApplicationLogLevel.Info,
            category,
            message));
    }

    public void Warning(string category, string message, Exception? exception = null)
    {
        Write(new ApplicationLogEntry(
            DateTimeOffset.Now,
            ApplicationLogLevel.Warning,
            category,
            message,
            exception));
    }

    public void Error(string category, string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Write(new ApplicationLogEntry(
            DateTimeOffset.Now,
            ApplicationLogLevel.Error,
            category,
            message,
            exception));
    }

    private static void Write(ApplicationLogEntry entry)
    {
        string line = $"[{entry.Timestamp:O}] [{entry.Level}] [{entry.Category}] {entry.Message}";
        Trace.WriteLine(line);

        if (entry.ExceptionDetails is not null)
        {
            Trace.WriteLine(entry.ExceptionDetails);
        }
    }
}
