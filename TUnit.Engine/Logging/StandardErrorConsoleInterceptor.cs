using TUnit.Core;
using TUnit.Engine.Services;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

internal class StandardErrorConsoleInterceptor : OptimizedConsoleInterceptor
{
    public static StandardErrorConsoleInterceptor Instance { get; private set; } = null!;

    public static TextWriter DefaultError { get; }

    protected override TextWriter RedirectedOut => Context.Current.ErrorOutputWriter;

    static StandardErrorConsoleInterceptor()
    {
        // Get the raw stream without SyncTextWriter synchronization wrapper
        // BufferedTextWriter already provides thread safety, so we avoid double-locking
        DefaultError = new StreamWriter(Console.OpenStandardError())
        {
            AutoFlush = true
        };
    }

    public StandardErrorConsoleInterceptor(VerbosityService verbosityService) : base(verbosityService)
    {
        Instance = this;
    }

    public void Initialize()
    {
        Console.SetError(this);
    }

    protected private override TextWriter GetOriginalOut()
    {
        return DefaultError;
    }

    protected private override void ResetDefault()
    {
        Console.SetError(DefaultError);
    }
}
