namespace TUnit.Core.Logging;

/// <summary>
/// Factory for configuring and managing log sinks.
/// </summary>
public static class TUnitLoggerFactory
{
    private static readonly List<ILogSink> _sinks = [];
    private static readonly Lock _lock = new();

    /// <summary>
    /// Registers a log sink to receive log messages.
    /// Call this in [Before(Assembly)] or before tests run.
    /// </summary>
    public static void AddSink(ILogSink sink)
    {
        lock (_lock)
        {
            _sinks.Add(sink);
        }
    }

    /// <summary>
    /// Registers a log sink by type. TUnit will instantiate it.
    /// </summary>
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
