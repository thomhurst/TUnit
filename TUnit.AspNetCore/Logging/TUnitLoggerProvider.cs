using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// A logger provider that creates <see cref="TUnitAspNetLogger"/> instances.
/// Logs are written to the current test's output.
/// </summary>
public sealed class TUnitLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TUnitAspNetLogger> _loggers = new();
    private readonly TestContext _testContext;
    private readonly LogLevel _minLogLevel;
    private bool _disposed;

    /// <summary>
    /// Creates a new TUnitLoggerProvider that uses the provided context provider
    /// to get the current test context.
    /// </summary>
    /// <param name="contextProvider">A function that returns the current test context, or null if not in a test.</param>
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
            name => new TUnitAspNetLogger(name, _testContext, _minLogLevel));
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
