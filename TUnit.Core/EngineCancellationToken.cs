namespace TUnit.Core;

/// <summary>
/// Represents a cancellation token for the engine.
/// </summary>
public class EngineCancellationToken : IDisposable
{
    /// <summary>
    /// Gets the internal cancellation token source.
    /// </summary>
    internal CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken Token { get; private set; }

    private volatile bool _forcefulExitStarted;

    /// <summary>
    /// Initializes the cancellation token with a linked token source.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to link with.</param>
    internal void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Token = CancellationTokenSource.Token;

        Token.Register(_ => Cancel(), this);

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

    private void Cancel()
    {
        // Cancel the test execution
        if (!CancellationTokenSource.IsCancellationRequested)
        {
            CancellationTokenSource.Cancel();
        }

        // Only start the forceful exit timer once
        if (!_forcefulExitStarted)
        {
            _forcefulExitStarted = true;

            // Start a new forceful exit timer
            _ = Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None).ContinueWith(t =>
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

            // Give After hooks a brief moment to execute via registered callbacks
            // ProcessExit has limited time, so we can only wait briefly
            Task.Delay(TimeSpan.FromMilliseconds(500)).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Disposes the cancellation token source.
    /// </summary>
    public void Dispose()
    {
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
