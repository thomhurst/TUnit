using System.Collections.Concurrent;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Arguments;

/// <summary>
/// A decorator matcher that delegates to an inner matcher and captures
/// argument values when the inner matcher returns <see langword="true"/>.
/// Every <see cref="Arg{T}"/> wraps its matcher in this decorator automatically.
/// </summary>
internal sealed class CapturingMatcher<T> : IArgumentMatcher<T>
{
    private readonly IArgumentMatcher _inner;
    private readonly ConcurrentQueue<T?> _captured = new();

    public CapturingMatcher(IArgumentMatcher inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<T?> CapturedValues => _captured.ToArray();

    public T? Latest
    {
        get
        {
            var snapshot = _captured.ToArray();
            return snapshot.Length > 0 ? snapshot[snapshot.Length - 1] : default;
        }
    }

    public bool Matches(T? value)
    {
        // Delegate to inner if it's typed
        bool result;
        if (_inner is IArgumentMatcher<T> typed)
        {
            result = typed.Matches(value);
        }
        else
        {
            result = _inner.Matches(value);
        }

        if (result)
        {
            _captured.Enqueue(value);
        }

        return result;
    }

    public bool Matches(object? value)
    {
        if (value is T typed)
        {
            return Matches(typed);
        }

        if (value is null)
        {
            // For reference types / nullable value types, delegate null
            var result = _inner.Matches(null);
            if (result)
            {
                _captured.Enqueue(default);
            }
            return result;
        }

        return _inner.Matches(value);
    }

    public string Describe() => _inner.Describe();
}
