using System.Collections.Concurrent;
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
    private ConcurrentQueue<T?>? _captured;
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
            var captured = Volatile.Read(ref _captured);
            if (captured is not null) return captured.ToArray();
            if (Volatile.Read(ref _initialCount) == 0) return Array.Empty<T?>();
            return GetInitialSnapshot();
        }
    }

    public T? Latest
    {
        get
        {
            var captured = Volatile.Read(ref _captured);
            if (captured is not null) return GetQueueLatest(captured);
            if (Volatile.Read(ref _initialCount) == 0) return default;

            var lockTaken = false;
            try
            {
                _captureLock.Enter(ref lockTaken);
                captured = _captured;
                if (captured is null) return _initialValues![_initialCount - 1];
            }
            finally
            {
                if (lockTaken) _captureLock.Exit(useMemoryBarrier: false);
            }
            return GetQueueLatest(captured);
        }
    }

    private static T? GetQueueLatest(ConcurrentQueue<T?> captured)
    {
        var snapshot = captured.ToArray();
        return snapshot.Length == 0 ? default : snapshot[^1];
    }

    private T?[] GetInitialSnapshot()
    {
        ConcurrentQueue<T?>? captured;
        var lockTaken = false;
        try
        {
            _captureLock.Enter(ref lockTaken);
            captured = _captured;
            if (captured is null)
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
        return captured.ToArray();
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
        var captured = Volatile.Read(ref _captured);
        if (captured is not null)
        {
            captured.Enqueue(value);
            return;
        }
        CaptureInitial(value);
    }

    // Most fresh mocks capture only one or two calls. Keep those in a small typed array, then
    // promote once to preserve ConcurrentQueue's append and contention costs for long histories.
    // Only the bounded initial storage and its promotion use this gate; no user code runs inside it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CaptureInitial(T? value)
    {
        var lockTaken = false;
        try
        {
            _captureLock.Enter(ref lockTaken);
            if (_captured is { } captured)
            {
                captured.Enqueue(value);
            }
            else if (_initialCount < 4)
            {
                _initialValues ??= new T?[4];
                _initialValues[_initialCount++] = value;
            }
            else
            {
                captured = new ConcurrentQueue<T?>(_initialValues!);
                captured.Enqueue(value);
                // Publish only after transferring every initial value and the triggering capture.
                Volatile.Write(ref _captured, captured);
                _initialValues = null;
            }
        }
        finally
        {
            if (lockTaken) _captureLock.Exit(useMemoryBarrier: false);
        }
    }
}
