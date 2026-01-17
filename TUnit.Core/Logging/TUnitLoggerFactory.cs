namespace TUnit.Core.Logging;

/// <summary>
/// Factory for configuring and managing log sinks in TUnit.
/// Use this class to register custom sinks that receive test output.
/// </summary>
/// <remarks>
/// <para>
/// TUnit uses a sink-based logging architecture where all test output
/// (Console.WriteLine, logger calls, etc.) is routed through registered sinks.
/// Each sink can handle the output differently - write to files, stream to IDEs,
/// send to external services, etc.
/// </para>
/// <para>
/// <strong>Built-in sinks (registered automatically):</strong>
/// <list type="bullet">
///   <item><description>TestOutputSink - Captures output for test results</description></item>
///   <item><description>ConsoleOutputSink - Real-time console output (when --output Detailed)</description></item>
///   <item><description>RealTimeOutputSink - Streams to IDE test explorers</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Register custom sinks in a [Before(Assembly)] hook:</strong>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a custom sink in an assembly hook
/// public class TestSetup
/// {
///     [Before(Assembly)]
///     public static void SetupLogging()
///     {
///         // Register by instance (for sinks needing configuration)
///         TUnitLoggerFactory.AddSink(new FileLogSink("test-output.log"));
///
///         // Or register by type (for simple sinks)
///         TUnitLoggerFactory.AddSink&lt;DebugLogSink&gt;();
///     }
/// }
///
/// // Simple sink that writes to Debug output
/// public class DebugLogSink : ILogSink
/// {
///     public bool IsEnabled(LogLevel level) =&gt; true;
///
///     public void Log(LogLevel level, string message, Exception? exception, Context? context)
///     {
///         System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
///     }
///
///     public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
///     {
///         Log(level, message, exception, context);
///         return ValueTask.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public static class TUnitLoggerFactory
{
    private static readonly List<ILogSink> _sinks = [];
    private static readonly Lock _lock = new();

    /// <summary>
    /// Registers a log sink instance to receive log messages from all tests.
    /// </summary>
    /// <param name="sink">The sink instance to register.</param>
    /// <remarks>
    /// <para>
    /// Call this method in a <c>[Before(Assembly)]</c> hook to ensure the sink
    /// is registered before any tests run.
    /// </para>
    /// <para>
    /// If your sink implements <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>,
    /// it will be automatically disposed when the test session ends.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Before(Assembly)]
    /// public static void SetupLogging()
    /// {
    ///     TUnitLoggerFactory.AddSink(new FileLogSink("test-output.log"));
    /// }
    /// </code>
    /// </example>
    public static void AddSink(ILogSink sink)
    {
        lock (_lock)
        {
            _sinks.Add(sink);
        }
    }

    /// <summary>
    /// Registers a log sink by type. TUnit will create a new instance using the parameterless constructor.
    /// </summary>
    /// <typeparam name="TSink">
    /// The sink type to register. Must implement <see cref="ILogSink"/> and have a parameterless constructor.
    /// </typeparam>
    /// <remarks>
    /// Use this overload for simple sinks that don't require constructor parameters.
    /// For sinks that need configuration, use <see cref="AddSink(ILogSink)"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Before(Assembly)]
    /// public static void SetupLogging()
    /// {
    ///     TUnitLoggerFactory.AddSink&lt;DebugLogSink&gt;();
    /// }
    /// </code>
    /// </example>
    public static void AddSink<TSink>() where TSink : ILogSink, new()
    {
        AddSink(new TSink());
    }

    /// <summary>
    /// Gets all registered sinks. For internal use.
    /// </summary>
    internal static IReadOnlyList<ILogSink> GetSinks()
    {
        lock (_lock)
        {
            return _sinks.ToArray();
        }
    }

    /// <summary>
    /// Disposes all sinks that implement IAsyncDisposable or IDisposable.
    /// Called at end of test session.
    /// </summary>
    internal static async ValueTask DisposeAllAsync()
    {
        ILogSink[] sinksToDispose;
        lock (_lock)
        {
            sinksToDispose = _sinks.ToArray();
            _sinks.Clear();
        }

        foreach (var sink in sinksToDispose)
        {
            try
            {
                if (sink is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (sink is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch
            {
                // Swallow disposal errors
            }
        }
    }

    /// <summary>
    /// Clears all registered sinks. For testing purposes.
    /// </summary>
    internal static void Clear()
    {
        lock (_lock)
        {
            _sinks.Clear();
        }
    }
}
