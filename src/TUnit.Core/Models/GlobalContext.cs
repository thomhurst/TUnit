using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;

namespace TUnit.Core;

public class GlobalContext : Context
{
    // Static, not AsyncLocal: a lazy-creating AsyncLocal getter poisons the first
    // reading branch with a fresh empty instance, hiding the framework's later
    // Current = ... assignment from that branch.
    private static GlobalContext? _current;
    public static new GlobalContext Current
    {
        // Factory overload — the parameterless one uses Activator.CreateInstance<T>(),
        // which AOT trimming may not preserve for GlobalContext's internal ctor.
        get => LazyInitializer.EnsureInitialized(ref _current, static () => new GlobalContext())!;
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
