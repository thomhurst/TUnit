using System.Collections.Concurrent;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Arguments;

/// <summary>
/// Non-generic interface for deferred capture after full setup match confirmation.
/// </summary>
internal interface ICapturingMatcher
{
    void ApplyCapture(object? value);
}

/// <summary>
/// A decorator matcher that delegates to an inner matcher and captures
/// argument values when the inner matcher returns <see langword="true"/>.
/// Every <see cref="Arg{T}"/> wraps its matcher in this decorator automatically.
/// </summary>
internal sealed class CapturingMatcher<T> : IArgumentMatcher<T>, ICapturingMatcher
{
    private readonly IArgumentMatcher _inner;
    private ConcurrentQueue<T?>? _captured;

    public CapturingMatcher(IArgumentMatcher inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<T?> CapturedValues => _captured?.ToArray() ?? Array.Empty<T?>();

    public T? Latest
    {
        get
        {
            if (_captured is null) return default;
            var snapshot = _captured.ToArray();
            return snapshot.Length > 0 ? snapshot[snapshot.Length - 1] : default;
        }
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
        if (_captured is null)
        {
            Interlocked.CompareExchange(ref _captured, new ConcurrentQueue<T?>(), null);
        }

        if (value is T typed)
        {
            _captured.Enqueue(typed);
        }
        else
        {
            _captured.Enqueue(default);
        }
    }

    public string Describe() => _inner.Describe();
}
