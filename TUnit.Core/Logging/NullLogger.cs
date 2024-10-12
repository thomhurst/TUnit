namespace TUnit.Core.Logging;

internal class NullLogger : ILogger
{
    public Task LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        return Task.CompletedTask;
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }
}