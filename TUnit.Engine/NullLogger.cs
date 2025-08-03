using Microsoft.Testing.Platform.Logging;

namespace TUnit.Engine;

internal class NullLogger<T> : ILogger<T>
{
    public Task LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Task.CompletedTask;

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel) => false;
}
