namespace TUnit.Core.Logging;

public interface ILogger
{
    /// <summary>
    /// Asynchronously logs a message with the specified log level, state, exception, and formatter.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logLevel">The log level of the message.</param>
    /// <param name="state">The state object.</param>
    /// <param name="exception">The exception associated with the message.</param>
    /// <param name="formatter">The formatter function to format the message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

    /// <summary>
    /// Logs a message with the specified log level, state, exception, and formatter.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logLevel">The log level of the message.</param>
    /// <param name="state">The state object.</param>
    /// <param name="exception">The exception associated with the message.</param>
    /// <param name="formatter">The formatter function to format the message.</param>
    void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

    /// <summary>
    /// Checks if the logger is enabled for the specified log level.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>True if the logger is enabled for the specified log level, otherwise false.</returns>
    bool IsEnabled(LogLevel logLevel);
}

/// <summary>
/// Represents a logger that can be used for logging messages with a specific category.
/// </summary>
/// <typeparam name="TCategoryName">The type of the category name.</typeparam>
public interface ILogger<out TCategoryName> : ILogger;
