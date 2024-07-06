namespace TUnit.Engine;

public static class EngineCancellationToken
{
    internal static CancellationTokenSource CancellationTokenSource { get; private set; } = new();
    public static TimedCancellationToken CreateToken(TimeSpan? timeout)
    {
        return new TimedCancellationToken(timeout);
    }

    internal static void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }
}