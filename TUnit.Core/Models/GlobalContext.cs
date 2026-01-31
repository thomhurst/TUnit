using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;

namespace TUnit.Core;

public class GlobalContext : Context
{
    private static readonly AsyncLocal<GlobalContext?> Contexts = new();
    public static new GlobalContext Current
    {
        get
        {
            return Contexts.Value ??= new GlobalContext();
        }
        internal set => Contexts.Value = value;
    }

    internal GlobalContext() : base(null)
    {
    }

    private ILogger _globalLogger = new Logging.EarlyBufferLogger();

    internal ILogger GlobalLogger
    {
        get => _globalLogger;
        set
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
