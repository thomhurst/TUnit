namespace TUnit.Core.Logging;

/// <summary>
/// Represents a destination for log messages. Implement this interface
/// to create custom log sinks that receive output from tests.
/// </summary>
/// <remarks>
/// <para>
/// Log sinks receive all output from:
/// <list type="bullet">
///   <item><description><c>Console.WriteLine()</c> calls during test execution</description></item>
///   <item><description><c>Console.Error.WriteLine()</c> calls (with <see cref="LogLevel.Error"/>)</description></item>
///   <item><description>TUnit logger output via <c>TestContext.Current.GetDefaultLogger()</c></description></item>
/// </list>
/// </para>
/// <para>
/// Register your sink in a <c>[Before(Assembly)]</c> hook or before tests run using
/// <see cref="TUnitLoggerFactory.AddSink(ILogSink)"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example: File logging sink
/// public class FileLogSink : ILogSink, IAsyncDisposable
/// {
///     private readonly StreamWriter _writer;
///
///     public FileLogSink(string path)
///     {
///         _writer = new StreamWriter(path, append: true);
///     }
///
///     public bool IsEnabled(LogLevel level) => level >= LogLevel.Information;
///
///     public void Log(LogLevel level, string message, Exception? exception, Context? context)
///     {
///         var testName = context is TestContext tc ? tc.TestDetails.TestName : "Unknown";
///         _writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] [{testName}] {message}");
///         if (exception != null)
///             _writer.WriteLine(exception.ToString());
///     }
///
///     public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
///     {
///         Log(level, message, exception, context);
///         return ValueTask.CompletedTask;
///     }
///
///     public async ValueTask DisposeAsync()
///     {
///         await _writer.FlushAsync();
///         await _writer.DisposeAsync();
///     }
/// }
///
/// // Register in assembly hook:
/// [Before(Assembly)]
/// public static void SetupLogging()
/// {
///     TUnitLoggerFactory.AddSink(new FileLogSink("test-output.log"));
/// }
/// </code>
/// </example>
public interface ILogSink
{
    /// <summary>
    /// Determines if this sink should receive messages at the specified level.
    /// Return <c>false</c> to skip processing for performance.
    /// </summary>
    /// <param name="level">The log level to check.</param>
    /// <returns><c>true</c> if messages at this level should be logged; otherwise <c>false</c>.</returns>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Synchronously logs a message to this sink.
    /// </summary>
    /// <param name="level">The log level (Information, Warning, Error, etc.).</param>
    /// <param name="message">The formatted message to log.</param>
    /// <param name="exception">Optional exception associated with this log entry.</param>
    /// <param name="context">
    /// The current execution context, which may be:
    /// <list type="bullet">
    ///   <item><description><see cref="TestContext"/> - during test execution</description></item>
    ///   <item><description><see cref="ClassHookContext"/> - during class hooks</description></item>
    ///   <item><description><see cref="AssemblyHookContext"/> - during assembly hooks</description></item>
    ///   <item><description><c>null</c> - if outside test execution</description></item>
    /// </list>
    /// </param>
    void Log(LogLevel level, string message, Exception? exception, Context? context);

    /// <summary>
    /// Asynchronously logs a message to this sink.
    /// </summary>
    /// <param name="level">The log level (Information, Warning, Error, etc.).</param>
    /// <param name="message">The formatted message to log.</param>
    /// <param name="exception">Optional exception associated with this log entry.</param>
    /// <param name="context">
    /// The current execution context, which may be:
    /// <list type="bullet">
    ///   <item><description><see cref="TestContext"/> - during test execution</description></item>
    ///   <item><description><see cref="ClassHookContext"/> - during class hooks</description></item>
    ///   <item><description><see cref="AssemblyHookContext"/> - during assembly hooks</description></item>
    ///   <item><description><c>null</c> - if outside test execution</description></item>
    /// </list>
    /// </param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context);
}
