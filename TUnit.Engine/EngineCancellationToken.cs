namespace TUnit.Engine;

public static class EngineCancellationToken
{
    internal static readonly CancellationTokenSource CancellationTokenSource = new();
    public static CancellationToken CreateToken(TimeSpan timeout)
    {
        var newCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token);
        newCancellationTokenSource.CancelAfter(timeout);
        return newCancellationTokenSource.Token;
    }
}