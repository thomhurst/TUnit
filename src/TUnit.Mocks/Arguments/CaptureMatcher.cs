using System.Runtime.CompilerServices;

namespace TUnit.Mocks.Arguments;

/// <summary>
/// Non-generic interface for deferred capture after full setup match confirmation.
/// </summary>
internal interface ICapturingMatcher
{
    void ApplyCapture(object? value);
}

/// <summary>
/// Generic interface for typed capture without boxing.
/// </summary>
internal interface ICapturingMatcher<in T>
{
    void ApplyCapture(T? value);
}

/// <summary>
/// A decorator matcher that delegates to an inner matcher and captures
/// argument values when the inner matcher returns <see langword="true"/>.
/// Every <see cref="Arg{T}"/> wraps its matcher in this decorator automatically.
/// </summary>
internal sealed class CapturingMatcher<T> : IArgumentMatcher<T>, ICapturingMatcher, ICapturingMatcher<T>
{
    private readonly IArgumentMatcher _inner;
    private CaptureSegment? _tail;
    private T?[]? _initialValues;
    private int _initialCount;
    private SpinLock _captureLock = new(enableThreadOwnerTracking: false);

    public CapturingMatcher(IArgumentMatcher inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<T?> CapturedValues
    {
        get
        {
            var tail = Volatile.Read(ref _tail);
            if (tail is not null) return tail.Snapshot();
            if (Volatile.Read(ref _initialCount) == 0) return Array.Empty<T?>();
            return GetInitialSnapshot();
        }
    }

    public T? Latest
    {
        get
        {
            var tail = Volatile.Read(ref _tail);
            if (tail is not null) return tail.Latest;
            if (Volatile.Read(ref _initialCount) == 0) return default;

            var lockTaken = false;
            try
            {
                _captureLock.Enter(ref lockTaken);
                tail = _tail;
                if (tail is null) return _initialValues![_initialCount - 1];
            }
            finally
            {
                if (lockTaken) _captureLock.Exit(useMemoryBarrier: false);
            }
            return tail.Latest;
        }
    }

    private T?[] GetInitialSnapshot()
    {
        CaptureSegment? tail;
        var lockTaken = false;
        try
        {
            _captureLock.Enter(ref lockTaken);
            tail = _tail;
            if (tail is null)
            {
                var snapshot = new T?[_initialCount];
                Array.Copy(_initialValues!, snapshot, _initialCount);
                return snapshot;
            }
        }
        finally
        {
            if (lockTaken) _captureLock.Exit(useMemoryBarrier: false);
        }
        return tail.Snapshot();
    }

    public bool Matches(T? value)
    {
        // Only test the inner matcher — do NOT capture here.
        // Capture is deferred until ApplyCapture() is called after
        // the full setup match is confirmed (all matchers passed).
        if (_inner is IArgumentMatcher<T> typed)
        {
            return typed.Matches(value);
        }

        return _inner.Matches(value);
    }

    public bool Matches(object? value)
    {
        if (value is T typed)
        {
            return Matches(typed);
        }

        if (value is null)
        {
            return _inner.Matches(null);
        }

        return _inner.Matches(value);
    }

    /// <summary>
    /// Captures the value after the full setup match has been confirmed.
    /// Called by <see cref="Setup.MethodSetup.ApplyCaptures"/>.
    /// </summary>
    void ICapturingMatcher.ApplyCapture(object? value)
    {
        Capture(value is T typed ? typed : default);
    }

    /// <summary>
    /// Typed capture path — avoids boxing for value types.
    /// </summary>
    void ICapturingMatcher<T>.ApplyCapture(T? value)
    {
        Capture(value);
    }

    public string Describe() => _inner.Describe();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Capture(T? value)
    {
        var tail = Volatile.Read(ref _tail);
        if (tail is null || !tail.TryAppend(value))
        {
            CaptureSlow(value);
        }
    }

    // Only initial captures and segment growth require the lock. Readers release
    // it before waiting on any reserved entries in a segment.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CaptureSlow(T? value)
    {
        var lockTaken = false;
        try
        {
            _captureLock.Enter(ref lockTaken);
            if (_tail is { } tail)
            {
                if (!tail.TryAppend(value))
                {
                    Volatile.Write(ref _tail, new CaptureSegment(tail, value));
                }
            }
            else if (_initialCount < 4)
            {
                _initialValues ??= new T?[4];
                _initialValues[_initialCount++] = value;
            }
            else
            {
                Volatile.Write(ref _tail, new CaptureSegment(_initialValues!, value));
                _initialValues = null;
            }
        }
        finally
        {
            if (lockTaken) _captureLock.Exit(useMemoryBarrier: false);
        }
    }

    // Reservations set the capture order. A reservation is published only after its
    // value is written, so readers can safely copy a wide struct from an immutable slot.
    private sealed class CaptureSegment
    {
        private const int InitialCapacity = 32;
        private const int MaximumCapacity = 1024 * 1024;

        private readonly CaptureSegment? _previous;
        private readonly long _previousCount;
        private readonly Entry[] _entries;
        private int _reserved;

        public CaptureSegment(T?[] initialValues, T? value)
        {
            _entries = new Entry[InitialCapacity];
            for (var i = 0; i < initialValues.Length; i++)
            {
                _entries[i] = new Entry { Value = initialValues[i], Published = true };
            }

            _entries[initialValues.Length] = new Entry { Value = value, Published = true };
            _reserved = initialValues.Length + 1;
        }

        public CaptureSegment(CaptureSegment previous, T? value)
        {
            _previous = previous;
            _previousCount = previous._previousCount + previous._entries.Length;
            _entries = new Entry[Math.Min(previous._entries.Length * 2, MaximumCapacity)];
            _entries[0] = new Entry { Value = value, Published = true };
            _reserved = 1;
        }

        public T? Latest => ReadPublished(ref _entries[Volatile.Read(ref _reserved) - 1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAppend(T? value)
        {
            while (true)
            {
                var index = Volatile.Read(ref _reserved);
                if (index == _entries.Length)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _reserved, index + 1, index) == index)
                {
                    ref var entry = ref _entries[index];
                    entry.Value = value;
                    Volatile.Write(ref entry.Published, true);
                    return true;
                }
            }
        }

        public T?[] Snapshot()
        {
            // Earlier segments are full. Only the tail can add reservations.
            var count = Volatile.Read(ref _reserved);
            var snapshot = new T?[_previousCount + count];
            for (var segment = this; ; segment = segment._previous)
            {
                var offset = segment._previousCount;
                for (var i = 0; i < count; i++)
                {
                    snapshot[offset + i] = ReadPublished(ref segment._entries[i]);
                }

                if (segment._previous is null)
                {
                    return snapshot;
                }

                count = segment._previous._entries.Length;
            }
        }

        private static T? ReadPublished(ref Entry entry)
        {
            var spinner = new SpinWait();
            while (!Volatile.Read(ref entry.Published))
            {
                spinner.SpinOnce();
            }

            return entry.Value;
        }

        private struct Entry
        {
            public T? Value;
            public bool Published;
        }
    }
}
