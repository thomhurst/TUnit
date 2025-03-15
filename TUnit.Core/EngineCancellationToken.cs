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
    
    /// <summary>
    /// Initializes the cancellation token with a linked token source.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to link with.</param>
    internal void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Token = CancellationTokenSource.Token;
    }

    /// <summary>
    /// Disposes the cancellation token source.
    /// </summary>
    public void Dispose()
    {
        CancellationTokenSource.Dispose();
    }
}