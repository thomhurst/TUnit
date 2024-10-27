namespace TUnit.Core;

public class EngineCancellationToken : IDisposable
{
    internal CancellationTokenSource CancellationTokenSource { get; private set; } = new();
    
    public CancellationToken Token { get; private set; }
    
    internal void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Token = CancellationTokenSource.Token;
    }

    public void Dispose()
    {
        CancellationTokenSource.Dispose();
    }
}