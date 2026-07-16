using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// A logger provider that creates <see cref="CorrelatedTUnitLogger"/> instances.
/// Each log call resolves the current test context dynamically via <see cref="TUnit.Core.TestContext.Current"/>,
/// supporting shared service scenarios where a single host serves multiple tests.
/// </summary>
public sealed class CorrelatedTUnitLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, CorrelatedTUnitLogger> _loggers = new();
    private readonly LogLevel _minLogLevel;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="CorrelatedTUnitLoggerProvider"/>.
    /// </summary>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    public CorrelatedTUnitLoggerProvider(LogLevel minLogLevel = LogLevel.Information)
    {
        _minLogLevel = minLogLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _loggers.GetOrAdd(categoryName,
            name => new CorrelatedTUnitLogger(name, _minLogLevel));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _loggers.Clear();
    }
}
