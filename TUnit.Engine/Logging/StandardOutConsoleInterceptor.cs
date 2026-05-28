using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

internal class StandardOutConsoleInterceptor : OptimizedConsoleInterceptor
{
    public static TextWriter DefaultOut { get; }

    // Console.Out is process-wide. Per-session interceptors must NOT clobber each other or
    // a sibling session's writes would stop being intercepted (#6001). We install exactly
    // one interceptor for the process and route through it; the interceptor itself is
    // stateless and dispatches to Context.Current (an AsyncLocal), so a single instance
    // serves every session safely.
    private static int s_installed;

    protected override LogLevel SinkLogLevel => LogLevel.Information;

    protected override ConsoleLineBuffer GetLineBuffer() => Context.Current.ConsoleStdOutLineBuffer;

    static StandardOutConsoleInterceptor()
    {
        DefaultOut = new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };
    }

    public void Initialize()
    {
        if (Interlocked.CompareExchange(ref s_installed, 1, 0) == 0)
        {
            Console.SetOut(this);
        }
    }

    private protected override TextWriter GetOriginalOut() => DefaultOut;

    private protected override void ResetDefault()
    {
        // No-op: we install exactly once per process and keep that interceptor live for the
        // lifetime of the process. Resetting Console.Out on a per-session dispose would tear
        // down interception for any concurrent sessions sharing the test host.
    }
}
