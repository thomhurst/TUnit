using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TUnit.Mock.Logging;

/// <summary>
/// A mock logger that captures all log entries for verification.
/// Implements <see cref="ILogger"/> directly — no source generation needed.
/// </summary>
public class MockLogger : ILogger
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly string? _categoryName;

    /// <summary>Creates a new mock logger.</summary>
    public MockLogger(string? categoryName = null)
    {
        _categoryName = categoryName;
    }

    /// <summary>All captured log entries in order.</summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            var list = new List<LogEntry>();
            foreach (var entry in _entries)
            {
                list.Add(entry);
            }
            return list;
        }
    }

    /// <summary>The most recently captured log entry, or null if none.</summary>
    public LogEntry? LatestEntry
    {
        get
        {
            LogEntry? latest = null;
            foreach (var entry in _entries)
            {
                latest = entry;
            }
            return latest;
        }
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _entries.Enqueue(new LogEntry(logLevel, eventId, message, exception, _categoryName));
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    /// <summary>Clears all captured log entries.</summary>
    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>
/// A mock logger that captures log entries and provides the typed <see cref="ILogger{T}"/> interface.
/// </summary>
public sealed class MockLogger<TCategoryName> : MockLogger, ILogger<TCategoryName>
{
    /// <summary>Creates a new mock logger for the specified category.</summary>
    public MockLogger() : base(typeof(TCategoryName).Name)
    {
    }
}
