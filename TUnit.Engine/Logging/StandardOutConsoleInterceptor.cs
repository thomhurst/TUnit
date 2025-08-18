using TUnit.Core;
using TUnit.Engine.Services;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

internal class StandardOutConsoleInterceptor : OptimizedConsoleInterceptor
{
    public static StandardOutConsoleInterceptor Instance { get; private set; } = null!;

    public static TextWriter DefaultOut { get; }

    protected override TextWriter RedirectedOut => Context.Current.OutputWriter;

    static StandardOutConsoleInterceptor()
    {
        // Get the raw stream without SyncTextWriter synchronization wrapper
        // BufferedTextWriter already provides thread safety, so we avoid double-locking
        DefaultOut = new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };
    }

    public StandardOutConsoleInterceptor(VerbosityService verbosityService) : base(verbosityService)
    {
        Instance = this;
    }

    public void Initialize()
    {
        Console.SetOut(this);
    }

    protected private override TextWriter GetOriginalOut()
    {
        return DefaultOut;
    }

    protected private override void ResetDefault()
    {
        Console.SetOut(DefaultOut);
    }
}
