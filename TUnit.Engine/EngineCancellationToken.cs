namespace TUnit.Engine;

public static class EngineCancellationToken
{
    internal static CancellationTokenSource CancellationTokenSource { get; private set; } = new();
    public static CancellationToken CreateToken(TimeSpan timeout)
    {
        var newCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token);
        newCancellationTokenSource.CancelAfter(timeout);
        return newCancellationTokenSource.Token;
    }

    internal static void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }
}