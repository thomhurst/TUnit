using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;

namespace TUnit.Core;

public class GlobalContext : Context
{
    // Process-level default instance used by source-gen module initializers that run
    // before any test session starts (and thus before any AsyncLocal value is set).
    private static readonly GlobalContext DefaultInstance = new();

    // AsyncLocal so concurrent sessions each get their own context without races.
    private static readonly AsyncLocal<GlobalContext?> Contexts = new();

    public static new GlobalContext Current
    {
        get => Contexts.Value ?? DefaultInstance;
        internal set => Contexts.Value = value;
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
