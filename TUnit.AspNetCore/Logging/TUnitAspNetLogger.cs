using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// A logger that writes log messages to TUnit's test output.
/// Messages are associated with the current test context for proper output capture.
/// </summary>
public sealed class TUnitAspNetLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TestContext _context;
    private readonly LogLevel _minLogLevel;

    internal TUnitAspNetLogger(string categoryName, TestContext context, LogLevel minLogLevel)
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

        // Set the current test context for proper output association
        TestContext.Current = _context;

        var message = formatter(state, exception);

        if (exception is not null)
        {
            message = $"{message}{Environment.NewLine}{exception}";
        }

        Console.WriteLine($"[{logLevel}] {_categoryName}: {message}");
    }
}
