using global::Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// A logger that writes log messages to TUnit's test output.
/// Sets <see cref="TestContext.Current"/> and writes via <see cref="Console"/> so
/// the console interceptor and all registered log sinks (test output, IDE real-time, console)
/// naturally pick up and route the output.
/// </summary>
public sealed class TUnitLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TestContext _context;
    private readonly LogLevel _minLogLevel;

    internal TUnitLogger(string categoryName, TestContext context, LogLevel minLogLevel)
    {
        _categoryName = categoryName;
        _context = context;
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

        // Set the current test context so the console interceptor routes output
        // to the correct test's sinks (test output, IDE real-time, console)
        TestContext.Current = _context;

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
