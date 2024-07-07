namespace TUnit.Engine;

public static class EngineCancellationToken
{
    internal static CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    internal static void Initialise(CancellationToken cancellationToken)
    {
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }
}