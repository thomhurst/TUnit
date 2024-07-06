namespace TUnit.Engine;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public class TimedCancellationToken
{
    private readonly TimeSpan? _timeout;

    public TimedCancellationToken(TimeSpan? timeout)
    {
        _timeout = timeout;
    }

    public static implicit operator CancellationToken(TimedCancellationToken cancellationToken)
    {
        var newCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(EngineCancellationToken.CancellationTokenSource.Token);
        
        if (cancellationToken._timeout != null)
        {
            newCancellationTokenSource.CancelAfter(cancellationToken._timeout.Value);
        }

        return newCancellationTokenSource.Token;
    }
}