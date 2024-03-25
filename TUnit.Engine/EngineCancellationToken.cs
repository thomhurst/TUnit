namespace TUnit.Engine;

public static class EngineCancellationToken
{
    internal static readonly CancellationTokenSource CancellationTokenSource = new();
    public static CancellationToken Token => CancellationTokenSource.Token;
}