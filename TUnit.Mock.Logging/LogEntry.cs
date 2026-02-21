using Microsoft.Extensions.Logging;

namespace TUnit.Mock.Logging;

/// <summary>
/// Represents a captured log entry from a mock logger.
/// </summary>
public sealed class LogEntry
{
    /// <summary>The log level of this entry.</summary>
    public LogLevel LogLevel { get; }

    /// <summary>The event ID associated with this entry.</summary>
    public EventId EventId { get; }

    /// <summary>The formatted log message.</summary>
    public string Message { get; }

    /// <summary>The exception associated with this entry, if any.</summary>
    public Exception? Exception { get; }

    /// <summary>The timestamp when this entry was recorded.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>The category name of the logger that produced this entry.</summary>
    public string? CategoryName { get; }

    public LogEntry(LogLevel logLevel, EventId eventId, string message, Exception? exception, string? categoryName)
    {
        LogLevel = logLevel;
        EventId = eventId;
        Message = message;
        Exception = exception;
        Timestamp = DateTimeOffset.UtcNow;
        CategoryName = categoryName;
    }

    /// <inheritdoc />
    public override string ToString()
        => Exception is null
            ? $"[{LogLevel}] {Message}"
            : $"[{LogLevel}] {Message} ({Exception.GetType().Name}: {Exception.Message})";
}
