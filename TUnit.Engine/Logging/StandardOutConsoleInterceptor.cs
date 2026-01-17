using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

internal class StandardOutConsoleInterceptor : OptimizedConsoleInterceptor
{
    public static StandardOutConsoleInterceptor Instance { get; private set; } = null!;

    public static TextWriter DefaultOut { get; }

    protected override LogLevel SinkLogLevel => LogLevel.Information;

    static StandardOutConsoleInterceptor()
    {
        DefaultOut = new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };
    }

    public StandardOutConsoleInterceptor()
    {
        Instance = this;
    }

    public void Initialize()
    {
        Console.SetOut(this);
    }

    private protected override TextWriter GetOriginalOut() => DefaultOut;

    private protected override void ResetDefault() => Console.SetOut(DefaultOut);
}
