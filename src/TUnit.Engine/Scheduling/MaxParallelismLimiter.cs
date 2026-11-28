namespace TUnit.Engine.Scheduling;

/// <summary>
/// The limits the session-wide parallel test budget moves between. A fixed budget has all
/// three equal.
/// </summary>
internal readonly record struct ParallelismLimits(int Initial, int Minimum, int Maximum)
{
    public static ParallelismLimits Fixed(int maxParallelism) => new(maxParallelism, maxParallelism, maxParallelism);

    public bool IsAdaptive => Minimum < Maximum;
}

/// <summary>
/// The session-wide --maximum-parallel-tests budget. The unconstrained phase, the keyed
/// NotInParallel phase and dynamic test batches can all run at the same time, so they
/// must draw from one budget for the configured cap to hold across all of them (#7074).
/// </summary>
/// <remarks>
/// <para>
/// Take a slot only immediately before entering <see cref="TestRunner"/>: never while
/// waiting on constraint keys. A dependency that a slot holder reaches first runs inline
/// within that holder's slot (via the <see cref="TestRunner"/> dedup ledger), so a slot
/// holder never waits on a test that still needs a slot of its own.
/// </para>
/// <para>
/// With adaptive parallelism the budget is resized while tests run (see
/// <see cref="SetLimit"/>). It never exceeds <see cref="MaxParallelism"/>.
/// </para>
/// </remarks>
internal sealed class MaxParallelismLimiter
{
    private readonly Lazy<ParallelismLimits> _limits;
    private readonly Lazy<SemaphoreSlim> _semaphore;
    private readonly Lock _resizeLock = new();

    // The number of slots the budget currently has. Starts at ParallelismLimits.Initial.
    private int _currentLimit;

    // Slots a shrink could not take back because tests were holding them. Each one is taken
    // back when a test releases its slot. Guarded by _resizeLock.
    private int _slotsToRetire;

    private long _releasedSlots;

    /// <summary>
    /// The cap used when nothing configures one, and for "unlimited" (0). A higher cap gains
    /// throughput only when nothing downstream limits concurrency. When a database pool, a
    /// container or a blocking call is the bottleneck, extra concurrent tests add no throughput:
    /// they wait inside their bodies, which inflates body time toward [Timeout].
    /// </summary>
    public static int DefaultMaxParallelism => Environment.ProcessorCount * 2;

    public MaxParallelismLimiter(Func<int> resolveMaxParallelism)
        : this(() => ParallelismLimits.Fixed(resolveMaxParallelism()))
    {
    }

    public MaxParallelismLimiter(Func<ParallelismLimits> resolveLimits)
    {
        _limits = new Lazy<ParallelismLimits>(resolveLimits);
        _semaphore = new Lazy<SemaphoreSlim>(() =>
        {
            var limits = _limits.Value;
            Volatile.Write(ref _currentLimit, limits.Initial);
            return new SemaphoreSlim(limits.Initial, limits.Maximum);
        });
    }

    public ParallelismLimits Limits => _limits.Value;

    /// <summary>
    /// The most tests that ever run at once: the configured cap, or the adaptive ceiling.
    /// </summary>
    public int MaxParallelism => _limits.Value.Maximum;

    /// <summary>
    /// The number of tests that may run at once right now.
    /// </summary>
    public int CurrentLimit
    {
        get
        {
            _ = _semaphore.Value;
            return Volatile.Read(ref _currentLimit);
        }
    }

    /// <summary>
    /// The number of slots no test holds right now. Zero means the limit is what holds tests back.
    /// </summary>
    public int AvailableSlots => _semaphore.Value.CurrentCount;

    /// <summary>
    /// The number of slots an adaptive budget has had released since the session started: one
    /// per finished test. A fixed budget does not count.
    /// </summary>
    public long ReleasedSlots => Volatile.Read(ref _releasedSlots);

    /// <summary>
    /// Waits for a slot. An uncontended wait completes synchronously without allocating.
    /// </summary>
    public Task WaitAsync(CancellationToken cancellationToken) => _semaphore.Value.WaitAsync(cancellationToken);

    public void Release()
    {
        // A fixed budget is never resized or measured, so its release takes no lock and
        // counts nothing: it does exactly what it did before the budget could be resized.
        if (!_limits.Value.IsAdaptive)
        {
            _semaphore.Value.Release();
            return;
        }

        // Ordered against SetLimit. Without the lock, a release could hand its slot to a
        // waiting test just after a shrink had counted that slot as one to retire, and the
        // lowered limit would admit one test too many.
        lock (_resizeLock)
        {
            _releasedSlots++;

            if (_slotsToRetire > 0)
            {
                // The slot leaves the budget instead of going to the next waiting test.
                _slotsToRetire--;
                return;
            }

            _semaphore.Value.Release();
        }
    }

    /// <summary>
    /// Resizes the budget, clamped to <see cref="Limits"/>, and returns the new limit. Growing
    /// takes effect at once. Shrinking takes free slots at once and the rest as running tests
    /// finish: a running test is never interrupted.
    /// </summary>
    public int SetLimit(int limit)
    {
        var limits = _limits.Value;
        var semaphore = _semaphore.Value;

        limit = Math.Min(Math.Max(limit, limits.Minimum), limits.Maximum);

        lock (_resizeLock)
        {
            var change = limit - _currentLimit;

            if (change > 0)
            {
                // Cancel pending retirements first: those slots are still held by tests and
                // return to the budget when released.
                var kept = Math.Min(change, _slotsToRetire);
                _slotsToRetire -= kept;
                change -= kept;

                if (change > 0)
                {
                    semaphore.Release(change);
                }
            }
            else
            {
                for (; change < 0; change++)
                {
                    // A zero timeout never waits: the task is already complete, with false when
                    // no slot is free. (SemaphoreSlim.Wait is unsupported on browser.)
                    var taken = semaphore.WaitAsync(0);
                    if (taken.Status != TaskStatus.RanToCompletion || !taken.Result)
                    {
                        _slotsToRetire++;
                    }
                }
            }

            Volatile.Write(ref _currentLimit, limit);
            return limit;
        }
    }
}
