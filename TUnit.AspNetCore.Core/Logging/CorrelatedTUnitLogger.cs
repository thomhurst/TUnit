using Microsoft.Extensions.Logging;
using TUnit.Core;
using TUnit.Logging.Microsoft;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// A logger that routes output to the current test via <see cref="TestContext.Current"/> (AsyncLocal),
/// which is set by the middleware via <see cref="TestContext.MakeCurrent"/>.
/// Writes via <see cref="Console"/> so the console interceptor and all registered log sinks
/// naturally route the output to the correct test.
/// </summary>
public sealed class CorrelatedTUnitLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogLevel _minLogLevel;

    internal CorrelatedTUnitLogger(string categoryName, LogLevel minLogLevel)
    {
        _categoryName = categoryName;
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

        var testContext = TestContext.Current;

        if (testContext is null)
        {
            return;
        }

        // Skip if a per-test logger is active for this test context
        // (avoids duplicate output when isolated factories inherit correlated logging)
        if (TUnitLoggingRegistry.PerTestLoggingActive.ContainsKey(testContext.Id))
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
            Console.Error.WriteLine(formattedMessage);
        }
        else
        {
            Console.WriteLine(formattedMessage);
        }
    }
}
