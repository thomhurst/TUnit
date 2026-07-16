using TUnit.Core.Settings;

namespace TUnit.Core;

/// <summary>
/// Represents a cancellation token for the engine.
/// </summary>
public class EngineCancellationToken : IDisposable
{
    /// <summary>
    /// Gets the internal cancellation token source.
    /// </summary>
    internal CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken Token { get; }

    private int _initialised;
    private volatile bool _forcefulExitStarted;
    private CancellationTokenRegistration _platformRegistration;

    public EngineCancellationToken()
    {
        Token = CancellationTokenSource.Token;
    }

    /// <summary>
    /// Hooks up process-wide cancellation signals (Ctrl+C / ProcessExit) the first time it's
    /// called for this instance. Idempotent — subsequent calls are no-ops so that concurrent
    /// MTP server-mode RPCs against one session don't clobber each other's cancellation chain.
    /// Per-call cancellation flows through the explicit <c>CancellationToken</c> threaded into
    /// discovery/execution, not through this session-scoped token.
    /// </summary>
    /// <param name="platformCancellationToken">
    /// Microsoft.Testing.Platform's run-abort token. The host (IDE stop button, CI runner cancel,
    /// <c>--abort</c>) cancels this independently of the OS signal handlers, so we link it in: when
    /// it fires, the engine token fires too, and session-scoped fixtures observing it tear down.
    /// Unlike the Ctrl+C path, this does NOT arm the forceful-exit timer — the platform owns
    /// process shutdown when it is the one cancelling, and killing the process underneath it would
    /// truncate result reporting.
    /// </param>
    internal void Initialise(CancellationToken platformCancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _initialised, 1, 0) != 0)
        {
            return;
        }

        if (platformCancellationToken.CanBeCanceled)
        {
            _platformRegistration = platformCancellationToken.Register(
                static state => ((EngineCancellationToken) state!).Cancel(armForcefulExit: false),
                this);
        }

        // Console.CancelKeyPress is not supported on browser platforms
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsBrowser())
        {
#endif
            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
#if NET5_0_OR_GREATER
        }
#endif
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Cancel();

        // Prevent the default behavior (immediate termination)
        e.Cancel = true;
    }

    private void Cancel(bool armForcefulExit = true)
    {
        // Cancel the test execution
        if (!CancellationTokenSource.IsCancellationRequested)
        {
            CancellationTokenSource.Cancel();
        }

        // The forceful-exit timer is only for signal-driven cancellation (Ctrl+C), where we
        // suppressed the runtime's default termination and must guarantee the process still dies.
        // Platform-driven cancellation manages its own shutdown, so it opts out.
        if (!armForcefulExit)
        {
            return;
        }

        // Only start the forceful exit timer once
        if (!_forcefulExitStarted)
        {
            _forcefulExitStarted = true;

            // Start a new forceful exit timer
            _ = Task.Delay(TUnitSettings.Default.Timeouts.ForcefulExitTimeout, CancellationToken.None).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    Console.WriteLine("Forcefully terminating the process due to cancellation request.");
                    Environment.Exit(1);
                }
            }, TaskScheduler.Default);
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        // Process is exiting (SIGTERM, kill, etc.) - trigger cancellation to execute After hooks
        // Note: ProcessExit runs on a background thread with limited time (~3 seconds on Windows)
        // The After hooks registered via CancellationToken.Register() will execute when we cancel
        if (!CancellationTokenSource.IsCancellationRequested)
        {
            CancellationTokenSource.Cancel();

            // Give After hooks a brief moment to execute via registered callbacks.
            // ProcessExit has limited time (~3s on Windows), so we can only wait briefly.
            // Thread.Sleep is appropriate here: we're on a synchronous event handler thread
            // and just need a simple delay — no need to involve the task scheduler.
            Thread.Sleep(TUnitSettings.Default.Timeouts.ProcessExitHookDelay);
        }
    }

    /// <summary>
    /// Disposes the cancellation token source.
    /// </summary>
    public void Dispose()
    {
        _platformRegistration.Dispose();

        // Console.CancelKeyPress is not supported on browser platforms
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsBrowser())
        {
#endif
            Console.CancelKeyPress -= OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
#if NET5_0_OR_GREATER
        }
#endif
        CancellationTokenSource.Dispose();
    }
}
