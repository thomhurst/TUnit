namespace TUnit.Core.Logging;

internal class NullLogger : ILogger
{
    public ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        return default(ValueTask);
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Trace;
    }
}
