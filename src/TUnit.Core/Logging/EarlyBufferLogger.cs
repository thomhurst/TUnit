using System.Collections.Concurrent;

namespace TUnit.Core.Logging;

/// <summary>
/// Logger that buffers messages until the real logger is configured.
/// Used as the initial GlobalLogger before TUnit infrastructure is set up.
/// </summary>
internal sealed class EarlyBufferLogger : ILogger
{
    private readonly ConcurrentQueue<(LogLevel level, string message)> _buffer = new();

    public bool IsEnabled(LogLevel logLevel) => true;

    public ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _buffer.Enqueue((logLevel, message));
        return ValueTask.CompletedTask;
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _buffer.Enqueue((logLevel, message));
    }

    /// <summary>
    /// Flushes all buffered messages to the provided logger.
    /// </summary>
    internal void FlushTo(ILogger logger)
    {
        while (_buffer.TryDequeue(out var entry))
        {
            var (level, message) = entry;
            switch (level)
            {
                case LogLevel.Trace:
                    logger.LogTrace(message);
                    break;
                case LogLevel.Debug:
                    logger.LogDebug(message);
                    break;
                case LogLevel.Information:
                    logger.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                    logger.LogError(message);
                    break;
                case LogLevel.Critical:
                    logger.LogCritical(message);
                    break;
            }
        }
    }
}
