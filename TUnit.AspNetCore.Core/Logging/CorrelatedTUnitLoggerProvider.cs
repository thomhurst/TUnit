using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// A logger provider that creates <see cref="CorrelatedTUnitLogger"/> instances.
/// Each log call resolves the current test context dynamically, supporting
/// shared web application scenarios where a single host serves multiple tests.
/// </summary>
public sealed class CorrelatedTUnitLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, CorrelatedTUnitLogger> _loggers = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LogLevel _minLogLevel;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="CorrelatedTUnitLoggerProvider"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor for resolving test context from requests.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    public CorrelatedTUnitLoggerProvider(IHttpContextAccessor httpContextAccessor, LogLevel minLogLevel = LogLevel.Information)
    {
        _httpContextAccessor = httpContextAccessor;
        _minLogLevel = minLogLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _loggers.GetOrAdd(categoryName,
            name => new CorrelatedTUnitLogger(name, _httpContextAccessor, _minLogLevel));
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
