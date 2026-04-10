using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// A logger provider that writes ILogger output to <see cref="Console"/> synchronously on the calling thread.
/// This is necessary because ASP.NET Core's built-in <c>ConsoleLogger</c> writes from a background queue thread
/// that does not inherit the <c>AsyncLocal</c> set by <see cref="TestContext.MakeCurrent"/>.
/// By writing on the request thread, TUnit's console interceptor can route the output to the correct test.
/// </summary>
internal sealed class SynchronousTUnitLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new SynchronousTUnitLogger(categoryName);

    public void Dispose() { }
}

internal sealed class SynchronousTUnitLogger : ILogger
{
    private readonly string _categoryName;

    internal SynchronousTUnitLogger(string categoryName) => _categoryName = categoryName;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information && TestContext.Current is not null;

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
            Console.Error.WriteLine(formattedMessage);
        }
        else
        {
            Console.WriteLine(formattedMessage);
        }
    }
}
