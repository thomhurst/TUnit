using System.Collections.Concurrent;

namespace TUnit.Mock.Arguments;

/// <summary>
/// Captures argument values passed to a mocked method during invocations.
/// Use with <see cref="Arg.Capture{T}"/> to capture values for later inspection.
/// </summary>
public class ArgCapture<T>
{
    private readonly ConcurrentQueue<T?> _values = new();

    /// <summary>Gets all captured values in order of capture.</summary>
    public IReadOnlyList<T?> Values => _values.ToArray();

    /// <summary>Gets the most recently captured value, or default if none captured.</summary>
    public T? Latest
    {
        get
        {
            var snapshot = _values.ToArray();
            return snapshot.Length > 0 ? snapshot[snapshot.Length - 1] : default;
        }
    }

    /// <summary>Adds a captured value. Called internally by CaptureMatcher.</summary>
    internal void Add(T? value) => _values.Enqueue(value);
}
