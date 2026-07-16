using Microsoft.Testing.Platform.Logging;

namespace TUnit.Engine;

/// <summary>
/// Null logger implementation for Microsoft Testing Platform's ILogger interface.
/// </summary>
internal class MtpNullLogger<T> : ILogger<T>
{
    public Task LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Task.CompletedTask;

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel) => false;
}
