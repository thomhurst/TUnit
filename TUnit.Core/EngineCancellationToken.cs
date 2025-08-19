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
    private bool _cancelKeyPressRegistered;

    /// <summary>
    /// Initializes the cancellation token with a linked token source.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to link with.</param>
    internal void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Token = CancellationTokenSource.Token;

        try
        {
            Console.CancelKeyPress += OnCancelKeyPress;
            _cancelKeyPressRegistered = true;
        }
        catch (PlatformNotSupportedException)
        {
            // Console.CancelKeyPress is not supported on some platforms (e.g., browser-wasm)
            // Continue without cancel key press handling
        }
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
                    Console.WriteLine("Forcefully terminating the process due to cancellation request.");
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
        if (_cancelKeyPressRegistered)
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
        _forcefulExitCts?.Cancel();
        _forcefulExitCts?.Dispose();
        CancellationTokenSource.Dispose();
    }
}
