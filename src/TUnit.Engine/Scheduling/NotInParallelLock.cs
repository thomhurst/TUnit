namespace TUnit.Engine.Scheduling;

/// <summary>
/// Async writer-prefer reader/writer primitive that enforces the runtime
/// "completely alone" semantic of global <c>[NotInParallel]</c>.
///
/// Phase ordering in <see cref="TestScheduler"/> drains the global NotInParallel
/// bucket last, but a Parallel-bucket test with <c>[DependsOn(NIPTest)]</c> can
/// trigger that NIPTest mid–parallel-phase via dependency recursion in
/// <see cref="TestRunner"/>. Without runtime exclusion the NIPTest then runs
/// alongside its siblings — violating the documented contract.
///
/// Acquisition must happen <em>after</em> dependency recursion, otherwise a
/// global-NIP test that depends on a Parallel test would deadlock against its
/// own dependency.
///
/// Suites with no global <c>[NotInParallel]</c> tests pay no overhead: the lock
/// stays disabled and Enter/Exit short-circuit. <see cref="TestScheduler"/>
/// flips the flag via <see cref="Enable"/> as soon as grouping reports any
/// global-NIP test (initial set or dynamic batch).
/// </summary>
internal sealed class NotInParallelLock : IDisposable
{
    private readonly SemaphoreSlim _writerGate = new(1, 1);
    private readonly Lock _stateLock = new();
    private int _activeReaders;
    private TaskCompletionSource? _drainTcs;
    private volatile bool _enabled;

    public void Enable() => _enabled = true;

    public void Dispose() => _writerGate.Dispose();

    public ValueTask<Scope> EnterAsync(bool exclusive, CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            return new ValueTask<Scope>(default(Scope));
        }
        return EnterEnabledAsync(exclusive, cancellationToken);
    }

    private async ValueTask<Scope> EnterEnabledAsync(bool exclusive, CancellationToken cancellationToken)
    {
        if (exclusive)
        {
            await EnterExclusiveAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await EnterSharedAsync(cancellationToken).ConfigureAwait(false);
        }
        return new Scope(this, exclusive);
    }

    private async Task EnterSharedAsync(CancellationToken cancellationToken)
    {
        await _writerGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_stateLock)
            {
                _activeReaders++;
            }
        }
        finally
        {
            _writerGate.Release();
        }
    }

    private void ExitShared()
    {
        TaskCompletionSource? toComplete = null;
        lock (_stateLock)
        {
            _activeReaders--;
            if (_activeReaders == 0 && _drainTcs is { } tcs)
            {
                toComplete = tcs;
                _drainTcs = null;
            }
        }
        toComplete?.TrySetResult();
    }

    private async Task EnterExclusiveAsync(CancellationToken cancellationToken)
    {
        await _writerGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        TaskCompletionSource? drainTcs;
        lock (_stateLock)
        {
            if (_activeReaders == 0)
            {
                // Already drained — keep _writerGate held until ExitExclusive so new
                // readers stay blocked while the writer's body runs.
                return;
            }
            drainTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _drainTcs = drainTcs;
        }

        var drained = false;
        try
        {
            using (cancellationToken.Register(static state =>
                       ((TaskCompletionSource)state!).TrySetCanceled(), drainTcs))
            {
                await drainTcs.Task.ConfigureAwait(false);
            }
            drained = true;
        }
        finally
        {
            if (!drained)
            {
                lock (_stateLock)
                {
                    if (ReferenceEquals(_drainTcs, drainTcs))
                    {
                        _drainTcs = null;
                    }
                }
                _writerGate.Release();
            }
        }
    }

    private void ExitExclusive()
    {
        _writerGate.Release();
    }

    internal readonly struct Scope(NotInParallelLock? owner, bool exclusive) : IDisposable
    {
        public void Dispose()
        {
            if (owner is null)
            {
                return;
            }
            if (exclusive)
            {
                owner.ExitExclusive();
            }
            else
            {
                owner.ExitShared();
            }
        }
    }
}
