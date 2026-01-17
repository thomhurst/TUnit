using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

internal class StandardErrorConsoleInterceptor : OptimizedConsoleInterceptor
{
    public static StandardErrorConsoleInterceptor Instance { get; private set; } = null!;

    public static TextWriter DefaultError { get; }

    protected override LogLevel SinkLogLevel => LogLevel.Error;

    static StandardErrorConsoleInterceptor()
    {
        DefaultError = new StreamWriter(Console.OpenStandardError())
        {
            AutoFlush = true
        };
    }

    public StandardErrorConsoleInterceptor()
    {
        Instance = this;
    }

    public void Initialize()
    {
        Console.SetError(this);
    }

    private protected override TextWriter GetOriginalOut() => DefaultError;

    private protected override void ResetDefault() => Console.SetError(DefaultError);
}
