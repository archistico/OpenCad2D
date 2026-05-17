using System;

namespace OpenCad2D.App.Diagnostics;

public sealed class ApplicationLogEntry
{
    public ApplicationLogEntry(
        DateTimeOffset timestamp,
        ApplicationLogLevel level,
        string category,
        string message,
        Exception? exception = null)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Log category cannot be empty.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Log message cannot be empty.", nameof(message));
        }

        Timestamp = timestamp;
        Level = level;
        Category = category;
        Message = message;
        Exception = exception;
    }

    public DateTimeOffset Timestamp { get; }

    public ApplicationLogLevel Level { get; }

    public string Category { get; }

    public string Message { get; }

    public Exception? Exception { get; }

    public string? ExceptionDetails => Exception?.ToString();
}
