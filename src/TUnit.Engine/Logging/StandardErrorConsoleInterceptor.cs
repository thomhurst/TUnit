using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

internal class StandardErrorConsoleInterceptor : OptimizedConsoleInterceptor
{
    public static TextWriter DefaultError { get; }

    // See StandardOutConsoleInterceptor for the rationale — Console.Error is also process-wide
    // and must be installed exactly once so concurrent sessions in one host (#6001) don't
    // clobber each other's interception.
    private static int s_installed;

    protected override LogLevel SinkLogLevel => LogLevel.Error;

    protected override ConsoleLineBuffer GetLineBuffer() => Context.Current.ConsoleStdErrLineBuffer;

    static StandardErrorConsoleInterceptor()
    {
        DefaultError = new StreamWriter(Console.OpenStandardError())
        {
            AutoFlush = true
        };
    }

    public void Initialize()
    {
        if (Interlocked.CompareExchange(ref s_installed, 1, 0) == 0)
        {
            Console.SetError(this);
        }
    }

    private protected override TextWriter GetOriginalOut() => DefaultError;

    private protected override void ResetDefault()
    {
        // No-op: we install exactly once per process. See StandardOutConsoleInterceptor.
    }
}
