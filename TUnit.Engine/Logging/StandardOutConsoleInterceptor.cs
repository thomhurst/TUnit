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
        DefaultOut = Console.Out;
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
