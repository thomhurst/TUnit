namespace TUnit.Core.Logging;

/// <summary>
/// Represents a destination for log messages. Implement this interface
/// to create custom log sinks (e.g., file, Seq, Application Insights).
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Asynchronously logs a message.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The formatted message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="context">The current context (TestContext, ClassHookContext, etc.), or null if outside test execution.</param>
    ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context);

    /// <summary>
    /// Synchronously logs a message.
    /// </summary>
    void Log(LogLevel level, string message, Exception? exception, Context? context);

    /// <summary>
    /// Determines if this sink should receive messages at the specified level.
    /// </summary>
    bool IsEnabled(LogLevel level);
}
