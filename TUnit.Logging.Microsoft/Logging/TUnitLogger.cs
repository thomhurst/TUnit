using global::Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// A logger that writes log messages to TUnit's test output.
/// Messages are written directly to the test context's output writers.
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

        var message = formatter(state, exception);

        if (exception is not null)
        {
            message = $"{message}{Environment.NewLine}{exception}";
        }

        var formattedMessage = $"[{logLevel}] {_categoryName}: {message}";

        if (logLevel >= LogLevel.Error)
        {
            _context.Output.WriteError(formattedMessage);
        }
        else
        {
            _context.Output.WriteLine(formattedMessage);
        }
    }
}
