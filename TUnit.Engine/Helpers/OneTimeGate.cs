using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Single-producer / multi-waiter gate for "run this work exactly once" semantics.
/// The first caller into <see cref="Run"/> executes <paramref name="work"/>; every
/// concurrent caller blocks on a <see cref="ManualResetEventSlim"/> until that work
/// finishes, with a bounded timeout so a stuck producer can't deadlock the rest of
/// the process. If the producing call throws, the exception is captured and
/// re-surfaced to every waiter so no concurrent caller silently proceeds past
/// partial state (#6001).
/// </summary>
internal sealed class OneTimeGate
{
    private readonly TimeSpan _waitTimeout;
    private readonly string _contextName;
    private int _started;
    private Exception? _failure;
    private readonly ManualResetEventSlim _completed = new(initialState: false);

    public OneTimeGate(string contextName, TimeSpan waitTimeout)
    {
        _contextName = contextName;
        _waitTimeout = waitTimeout;
    }

    [UnconditionalSuppressMessage("Platform", "CA1416:Validate platform compatibility", Justification = "TUnit does not run on browser targets; callers of OneTimeGate are not reachable there.")]
    public void Run(Action work)
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            if (!_completed.Wait(_waitTimeout))
            {
                throw new TimeoutException(
                    $"{_contextName} timed out waiting for the first caller to complete after {_waitTimeout.TotalSeconds:0}s. " +
                    "This usually indicates a hang in the work being performed.");
            }

            if (_failure is { } firstFailure)
            {
                throw new InvalidOperationException(
                    $"{_contextName} failed in the producing caller; see inner exception.",
                    firstFailure);
            }

            return;
        }

        try
        {
            work();
        }
        catch (Exception ex)
        {
            _failure = ex;
            throw;
        }
        finally
        {
            _completed.Set();
        }
    }
}
