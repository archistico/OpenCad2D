using System;
using System.Collections.Generic;

namespace OpenCad2D.App.Diagnostics;

public sealed class InMemoryApplicationLogger : IApplicationLogger
{
    private readonly object _syncRoot = new();
    private readonly List<ApplicationLogEntry> _entries = new();

    public IReadOnlyList<ApplicationLogEntry> Entries
    {
        get
        {
            lock (_syncRoot)
            {
                return _entries.ToArray();
            }
        }
    }

    public void Info(string category, string message)
    {
        Add(ApplicationLogLevel.Info, category, message);
    }

    public void Warning(string category, string message, Exception? exception = null)
    {
        Add(ApplicationLogLevel.Warning, category, message, exception);
    }

    public void Error(string category, string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Add(ApplicationLogLevel.Error, category, message, exception);
    }

    private void Add(
        ApplicationLogLevel level,
        string category,
        string message,
        Exception? exception = null)
    {
        var entry = new ApplicationLogEntry(
            DateTimeOffset.Now,
            level,
            category,
            message,
            exception);

        lock (_syncRoot)
        {
            _entries.Add(entry);
        }
    }
}
