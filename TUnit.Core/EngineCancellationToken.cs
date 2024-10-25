namespace TUnit.Core;

public class EngineCancellationToken
{
    internal CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    public CancellationToken Token => CancellationTokenSource.Token;
    
    internal void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }
}