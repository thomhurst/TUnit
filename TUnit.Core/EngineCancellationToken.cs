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
    
    private CancellationTokenSource? _forcefulExitCts;
    private volatile bool _forcefulExitStarted;

    /// <summary>
    /// Initializes the cancellation token with a linked token source.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to link with.</param>
    internal void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Token = CancellationTokenSource.Token;

        // Console.CancelKeyPress is not supported on browser platforms
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsBrowser())
        {
#endif
            Console.CancelKeyPress += OnCancelKeyPress;
#if NET5_0_OR_GREATER
        }
#endif
    }
    
    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
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
            
            // Cancel any previous forceful exit timer
            _forcefulExitCts?.Cancel();
            _forcefulExitCts?.Dispose();
            _forcefulExitCts = new CancellationTokenSource();
            
            // Start a new forceful exit timer
            _ = Task.Delay(TimeSpan.FromSeconds(10), _forcefulExitCts.Token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    Environment.Exit(1);
                }
            }, TaskScheduler.Default);
        }
        
        // Prevent the default behavior (immediate termination)
        e.Cancel = true;
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
#if NET5_0_OR_GREATER
        }
#endif
        _forcefulExitCts?.Cancel();
        _forcefulExitCts?.Dispose();
        CancellationTokenSource.Dispose();
    }
}
