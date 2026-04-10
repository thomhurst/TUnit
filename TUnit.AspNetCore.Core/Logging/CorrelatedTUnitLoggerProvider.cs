using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// A logger provider that creates <see cref="CorrelatedTUnitLogger"/> instances.
/// Each log call resolves the current test context dynamically via <see cref="TestContextResolverRegistry"/>,
/// supporting shared web application scenarios where a single host serves multiple tests.
/// </summary>
/// <remarks>
/// Each provider instance registers its own <see cref="HttpContextTestContextResolver"/> in the global
/// <see cref="TestContextResolverRegistry"/> and unregisters it on <see cref="Dispose"/>. In multi-factory
/// scenarios, each factory's provider adds one resolver — this is correct because each resolver queries
/// its own factory's <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
/// </remarks>
public sealed class CorrelatedTUnitLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, CorrelatedTUnitLogger> _loggers = new();
    private readonly HttpContextTestContextResolver _resolver;
    private readonly LogLevel _minLogLevel;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="CorrelatedTUnitLoggerProvider"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor used to resolve test context from incoming requests.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    public CorrelatedTUnitLoggerProvider(IHttpContextAccessor httpContextAccessor, LogLevel minLogLevel = LogLevel.Information)
    {
        _minLogLevel = minLogLevel;
        _resolver = new HttpContextTestContextResolver(httpContextAccessor);
        TestContextResolverRegistry.Register(_resolver);
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
        TestContextResolverRegistry.Unregister(_resolver);
        _loggers.Clear();
    }
}
