using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TUnit.Core;
using TUnit.Logging.Microsoft;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// A logger that resolves the current test context per log call, supporting shared web application scenarios.
/// The resolution chain is:
/// <list type="number">
///   <item>Test context from <see cref="HttpContext.Items"/> (set by <see cref="TUnitTestContextMiddleware"/>)</item>
///   <item><see cref="TestContext.Current"/> (AsyncLocal fallback)</item>
///   <item>No-op if no test context is available</item>
/// </list>
/// </summary>
public sealed class CorrelatedTUnitLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LogLevel _minLogLevel;

    internal CorrelatedTUnitLogger(string categoryName, IHttpContextAccessor httpContextAccessor, LogLevel minLogLevel)
    {
        _categoryName = categoryName;
        _httpContextAccessor = httpContextAccessor;
        _minLogLevel = minLogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return TUnitLoggerScope.Push(state);
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var testContext = ResolveTestContext();

        if (testContext is null)
        {
            return;
        }

        var message = formatter(state, exception);

        if (exception is not null)
        {
            message = $"{message}{Environment.NewLine}{exception}";
        }

        var formattedMessage = $"[{logLevel}] {_categoryName}: {message}";

        if (logLevel >= LogLevel.Error)
        {
            testContext.Output.WriteError(formattedMessage);
        }
        else
        {
            testContext.Output.WriteLine(formattedMessage);
        }
    }

    private TestContext? ResolveTestContext()
    {
        // 1. Try to get from HttpContext.Items (set by TUnitTestContextMiddleware)
        if (_httpContextAccessor.HttpContext?.Items[TUnitTestContextMiddleware.HttpContextKey] is TestContext httpTestContext)
        {
            return httpTestContext;
        }

        // 2. Fall back to AsyncLocal
        return TestContext.Current;
    }
}
