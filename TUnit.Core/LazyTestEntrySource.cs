using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Lazy wrapper around TestEntry&lt;T&gt;[] factories for deferred JIT compilation.
/// Factories are only invoked on first access to Count, GetFilterData, or Materialize,
/// avoiding per-class static constructor execution during module initialization.
/// Supports multiple factories for the same T (e.g. generic classes with multiple concrete instantiations).
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class LazyTestEntrySource<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods)] T> : ITestEntrySource where T : class
{
    private List<Func<TestEntry<T>[]>>? _factories;
    private volatile TestEntrySource<T>? _resolved;
    private readonly object _lock = new();

    public LazyTestEntrySource(Func<TestEntry<T>[]> factory)
    {
        _factories = [factory];
    }

    /// <summary>
    /// Adds another factory for the same T. Used when multiple source-gen files
    /// register entries for the same class (e.g. generic instantiations).
    /// Must be called before first resolution.
    /// </summary>
    internal void AddFactory(Func<TestEntry<T>[]> factory)
    {
        lock (_lock)
        {
            if (_resolved is not null)
            {
                // Already resolved — fall back to eager merge on the resolved source
                _resolved.AddEntries(factory());
                return;
            }

            _factories!.Add(factory);
        }
    }

    private TestEntrySource<T> Resolve()
    {
        if (_resolved is not null)
        {
            return _resolved;
        }

        lock (_lock)
        {
            if (_resolved is not null)
            {
                return _resolved;
            }

            var factories = _factories!;
            TestEntry<T>[] entries;

            if (factories.Count == 1)
            {
                entries = factories[0]();
            }
            else
            {
                var allEntries = new List<TestEntry<T>>();
                foreach (var factory in factories)
                {
                    allEntries.AddRange(factory());
                }
                entries = allEntries.ToArray();
            }

            if (entries.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Source-generated test registration failed: no entries for '{typeof(T).FullName}'. " +
                    "This indicates a source generator bug. Please report this issue.");
            }

            _resolved = new TestEntrySource<T>(entries);
            _factories = null; // Release factory references
            return _resolved;
        }
    }

    public int Count => Resolve().Count;
    public Type ClassType => typeof(T);
    public string ClassName => Resolve().ClassName;

    public TestEntryFilterData GetFilterData(int index) => Resolve().GetFilterData(index);

    public IReadOnlyList<TestMetadata> Materialize(int index, string testSessionId)
        => Resolve().Materialize(index, testSessionId);
}
