using System.Collections.Concurrent;
using global::Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// A logger provider that creates <see cref="TUnitLogger"/> instances.
/// Logs are written directly to the test context's output.
/// </summary>
public sealed class TUnitLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TUnitLogger> _loggers = new();
    private readonly TestContext _testContext;
    private readonly LogLevel _minLogLevel;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="TUnitLoggerProvider"/> that writes logs to the specified test context.
    /// </summary>
    /// <param name="testContext">The test context to write logs to.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    public TUnitLoggerProvider(TestContext testContext, LogLevel minLogLevel = LogLevel.Information)
    {
        _testContext = testContext;
        _minLogLevel = minLogLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _loggers.GetOrAdd(categoryName,
            name => new TUnitLogger(name, _testContext, _minLogLevel));
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
