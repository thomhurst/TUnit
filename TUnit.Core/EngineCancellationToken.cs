namespace TUnit.Core;

public static class EngineCancellationToken
{
    internal static CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    public static CancellationToken Token => CancellationTokenSource.Token;
    
    internal static void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }
}