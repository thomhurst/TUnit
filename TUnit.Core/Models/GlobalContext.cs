using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;

namespace TUnit.Core;

public class GlobalContext : Context
{
    // Per-process, not per-async-context: GlobalContext is the session-wide root and
    // must be visible from any thread/async branch (test discovery hooks, parallel
    // hook execution, MTP test-host-controller mode under --hangdump). The previous
    // AsyncLocal getter mutated `Contexts.Value` on first read, poisoning that
    // async branch with a fresh empty instance and preventing the framework's later
    // assignment from being observed there.
    private static GlobalContext? _current;
    public static new GlobalContext Current
    {
        get => LazyInitializer.EnsureInitialized(ref _current)!;
        internal set => Volatile.Write(ref _current, value);
    }

    internal GlobalContext() : base(null)
    {
    }

    private ILogger _globalLogger = new Logging.EarlyBufferLogger();

    public ILogger GlobalLogger
    {
        get => _globalLogger;
        internal set
        {
            // Flush buffered logs to the new logger
            if (_globalLogger is Logging.EarlyBufferLogger bufferLogger)
            {
                bufferLogger.FlushTo(value);
            }

            _globalLogger = value;
        }
    }

    public string? TestFilter { get; internal set; }
    public TextWriter OriginalConsoleOut { get; set; } = Console.Out;
    public TextWriter OriginalConsoleError { get; set; } = Console.Error;

    [field: AllowNull, MaybeNull]
    internal Disposer Disposer
    {
        get => field ??= new Disposer(GlobalLogger);
        set;
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
