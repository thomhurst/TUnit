using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Thin non-generic wrapper around TestEntry&lt;T&gt;[] for engine access.
/// Holds the lightweight filter data eagerly (cheap — string/int literals only),
/// and one or more factories that produce the full <see cref="TestEntry{T}"/> array.
/// The factories are only invoked once a test is selected for materialization,
/// so discovery filtering never triggers the per-class delegate/metadata <c>.cctor</c>.
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class TestEntrySource<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods)] T> : ITestEntrySource where T : class
{
    private volatile TestEntryFilterData[]? _filterData;
    private List<Func<TestEntryFilterData[]>>? _filterDataFactories;
    private List<Func<TestEntry<T>[]>>? _factories;
    private volatile TestEntry<T>[]? _entries;
    private string? _className;
    private readonly object _lock = new();

    public TestEntrySource(Func<TestEntryFilterData[]> filterDataFactory, Func<TestEntry<T>[]> factory)
    {
        _filterDataFactories = [filterDataFactory];
        _factories = [factory];
    }

    /// <summary>
    /// Adds another (filterDataFactory, factory) pair for the same T. Used when multiple source-gen
    /// files register entries for the same class (e.g. generic instantiations).
    /// Thread-safe via lock since static field initializers may run concurrently.
    /// </summary>
    internal void AddSource(Func<TestEntryFilterData[]> filterDataFactory, Func<TestEntry<T>[]> factory)
    {
        lock (_lock)
        {
            if (_filterData is not null)
            {
                // Filter data already resolved — merge eagerly to keep indexing consistent.
                _filterData = [.. _filterData, .. filterDataFactory()];
            }
            else
            {
                _filterDataFactories!.Add(filterDataFactory);
            }

            if (_entries is not null)
            {
                // Entries already resolved — merge eagerly to keep ordering consistent.
                _entries = [.. _entries, .. factory()];
                return;
            }

            _factories!.Add(factory);
        }
    }

    private TestEntryFilterData[] ResolveFilterData()
    {
        if (_filterData is not null)
        {
            return _filterData;
        }

        lock (_lock)
        {
            if (_filterData is not null)
            {
                return _filterData;
            }

            var factories = _filterDataFactories!;
            TestEntryFilterData[] data;
            if (factories.Count == 1)
            {
                data = factories[0]();
            }
            else
            {
                var list = new List<TestEntryFilterData>();
                foreach (var f in factories)
                {
                    list.AddRange(f());
                }
                data = list.ToArray();
            }

            _filterData = data;
            _filterDataFactories = null;
            return data;
        }
    }

    private TestEntry<T>[] Resolve()
    {
        if (_entries is not null)
        {
            return _entries;
        }

        lock (_lock)
        {
            if (_entries is not null)
            {
                return _entries;
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

            _entries = entries;
            _factories = null;
            return entries;
        }
    }

    public Type ClassType => typeof(T);
    public string ClassName => _className ??= TUnit.Core.Extensions.TestContextExtensions.GetNestedTypeName(typeof(T));

    public TestEntryFilterData[] FilterData => _filterData ?? ResolveFilterData();

    public TestMetadata Materialize(int index, string testSessionId)
    {
        return Resolve()[index].ToTestMetadata(testSessionId);
    }
}
