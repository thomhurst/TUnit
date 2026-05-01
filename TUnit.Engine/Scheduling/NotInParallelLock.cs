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
/// </summary>
internal sealed class NotInParallelLock
{
    private readonly SemaphoreSlim _writerGate = new(1, 1);
    private readonly Lock _stateLock = new();
    private int _activeReaders;
    private TaskCompletionSource? _drainTcs;

    public async ValueTask<Scope> EnterAsync(bool exclusive, CancellationToken cancellationToken)
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
                return;
            }
            drainTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _drainTcs = drainTcs;
        }

        try
        {
            using (cancellationToken.Register(static state =>
                       ((TaskCompletionSource)state!).TrySetCanceled(), drainTcs))
            {
                await drainTcs.Task.ConfigureAwait(false);
            }
        }
        catch
        {
            lock (_stateLock)
            {
                if (ReferenceEquals(_drainTcs, drainTcs))
                {
                    _drainTcs = null;
                }
            }
            _writerGate.Release();
            throw;
        }
    }

    private void ExitExclusive()
    {
        _writerGate.Release();
    }

    internal readonly struct Scope(NotInParallelLock owner, bool exclusive) : IDisposable
    {
        public void Dispose()
        {
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
