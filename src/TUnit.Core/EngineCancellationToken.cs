#if NET
using System.Runtime.InteropServices;
#endif
using System.Runtime.Versioning;
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
    private readonly object _forcefulExitGate = new();
    private bool _disposed;
    private CancellationTokenRegistration _platformRegistration;
    private bool _cancelKeyPressSubscribed;
#if NET
    private PosixSignalRegistration? _sigtermRegistration;
    private volatile bool _terminationSignalReceived;
#endif

    /// <summary>
    /// Whether the run was cancelled by SIGTERM. Microsoft.Testing.Platform does not observe that
    /// signal, so unlike Ctrl+C its own run token is not cancelled alongside ours.
    /// </summary>
    internal bool TerminationSignalReceived =>
#if NET
        _terminationSignalReceived;
#else
        false;
#endif

    public EngineCancellationToken()
    {
        Token = CancellationTokenSource.Token;
    }

    /// <summary>
    /// Hooks up process-wide cancellation signals (Ctrl+C / SIGTERM / ProcessExit) the first time it's
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
            _cancelKeyPressSubscribed = TrySubscribeCancelKeyPress();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
#if NET5_0_OR_GREATER
        }
#endif

#if NET
        _sigtermRegistration = TryRegisterSigterm();
#endif
    }

#if NET
    /// <summary>
    /// SIGTERM is how Unix asks a process to stop (<c>docker stop</c>, Kubernetes, CI cancellation,
    /// <c>timeout</c>). Left to the runtime it only raises <see cref="AppDomain.ProcessExit"/>, which
    /// gives After hooks <see cref="TimeoutSettings.ProcessExitHookDelay"/> before the process
    /// dies. Handling it like Ctrl+C instead cancels the run and allows the full
    /// <see cref="TimeoutSettings.ForcefulExitTimeout"/> for cleanup. Windows is left alone:
    /// there the signal maps to console close/shutdown events, which the runtime already handles.
    /// </summary>
    private PosixSignalRegistration? TryRegisterSigterm()
    {
        // Android, iOS and tvOS do not deliver POSIX signals to managed code.
        if (OperatingSystem.IsWindows() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi()
            || OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
        {
            return null;
        }

        try
        {
            return PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSigterm);
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }

    private void OnSigterm(PosixSignalContext context)
    {
        // Suppress the default behaviour (immediate termination) only when a live session took the
        // signal: the forceful-exit timer it armed still guarantees the process ends. A signal that
        // lands after disposal keeps the runtime's default, so the process still stops.
        // Only ever set it: every registered handler shares this context, and assigning false here
        // would undo another, live session's suppression.
        if (OnTerminationSignal())
        {
            context.Cancel = true;
        }
    }

    /// <returns>Whether a live session handled the signal (and armed the forceful-exit timer).</returns>
    internal bool OnTerminationSignal()
    {
        // Check-and-arm under the gate Dispose takes: either the session is already disposed and the
        // signal is not handled, or the timer is armed before Dispose can complete.
        lock (_forcefulExitGate)
        {
            if (_disposed)
            {
                return false;
            }

            _terminationSignalReceived = true;

            if (!_forcefulExitStarted)
            {
                _forcefulExitStarted = true;
                ArmForcefulExit();
            }
        }

        // Runs cancellation callbacks (After hooks) synchronously on the signal-dispatch thread. A
        // slow hook blocks that thread, which is acceptable: the timer armed above bounds it.
        try
        {
            Cancel(armForcefulExit: false);
        }
        catch (ObjectDisposedException)
        {
            // Disposing the registration does not stop a dispatch the runtime had already
            // snapshotted, so the signal can land after the session's token was disposed.
            // An exception escaping the signal handler would crash the process.
        }

        return true;
    }
#endif

    /// <summary>
    /// Subscribes to <see cref="Console.CancelKeyPress"/>. Platforms without console signals
    /// (such as Android, iOS and tvOS) throw <see cref="PlatformNotSupportedException"/>; there the
    /// run is cancelled only through the platform token.
    /// </summary>
#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    private bool TrySubscribeCancelKeyPress()
    {
        try
        {
            SubscribeCancelKeyPress();
            return true;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    // Virtual so tests can simulate a platform without console signals.
#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    internal virtual void SubscribeCancelKeyPress() => Console.CancelKeyPress += OnCancelKeyPress;

#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    internal virtual void UnsubscribeCancelKeyPress() => Console.CancelKeyPress -= OnCancelKeyPress;

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Cancel();

        // Prevent the default behavior (immediate termination)
        e.Cancel = true;
    }

    private void Cancel(bool armForcefulExit = true)
    {
        // The forceful-exit timer is only for signal-driven cancellation (Ctrl+C / SIGTERM), where we
        // suppressed the runtime's default termination and must guarantee the process still dies.
        // Platform-driven cancellation manages its own shutdown, so it opts out.
        // Arm it BEFORE cancelling: Cancel() runs registered callbacks (After hooks) synchronously,
        // and one that blocks must not postpone the deadline that is meant to bound it.
        if (armForcefulExit)
        {
            // Under the gate shared with Dispose: a signal the runtime dispatches after the session
            // was disposed must not arm a timer that would later kill a host still in use.
            lock (_forcefulExitGate)
            {
                if (!_disposed && !_forcefulExitStarted)
                {
                    _forcefulExitStarted = true;
                    ArmForcefulExit();
                }
            }
        }

        // Cancel the test execution
        if (!CancellationTokenSource.IsCancellationRequested)
        {
            CancellationTokenSource.Cancel();
        }
    }

    // Virtual so tests can observe arming without terminating the test process.
    internal virtual void ArmForcefulExit()
    {
        _ = Task.Delay(TUnitSettings.Default.Timeouts.ForcefulExitTimeout, CancellationToken.None).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                Console.WriteLine("Forcefully terminating the process due to cancellation request.");
                Environment.Exit(1);
            }
        }, TaskScheduler.Default);
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        // Process is exiting (Environment.Exit, end of Main, or a signal not handled above) - trigger
        // cancellation to execute After hooks.
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
        lock (_forcefulExitGate)
        {
            _disposed = true;
        }

        _platformRegistration.Dispose();
#if NET
        _sigtermRegistration?.Dispose();
        _sigtermRegistration = null;
#endif

        // Console.CancelKeyPress is not supported on browser platforms
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsBrowser())
        {
#endif
            if (_cancelKeyPressSubscribed)
            {
                _cancelKeyPressSubscribed = false;
                UnsubscribeCancelKeyPress();
            }

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
#if NET5_0_OR_GREATER
        }
#endif
        CancellationTokenSource.Dispose();
    }
}
