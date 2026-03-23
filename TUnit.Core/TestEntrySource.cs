using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Thin non-generic wrapper around TestEntry&lt;T&gt;[] for engine access.
/// Provides filter data extraction and delegates materialization to TestEntry&lt;T&gt;.ToTestMetadata().
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class TestEntrySource<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods)] T> : ITestEntrySource where T : class
{
    private TestEntry<T>[] _entries;
    private readonly string _className;

    public TestEntrySource(TestEntry<T>[] entries)
    {
        _entries = entries;
        _className = TUnit.Core.Extensions.TestContextExtensions.GetNestedTypeName(typeof(T));
    }

    /// <summary>
    /// Appends additional entries. Used when multiple source-gen files register entries for the same T.
    /// Thread-safe via lock since static field initializers may run concurrently.
    /// </summary>
    internal void AddEntries(TestEntry<T>[] additional)
    {
        lock (_lock)
        {
            var combined = new TestEntry<T>[_entries.Length + additional.Length];
            Array.Copy(_entries, 0, combined, 0, _entries.Length);
            Array.Copy(additional, 0, combined, _entries.Length, additional.Length);
            _entries = combined;
        }
    }

    private readonly object _lock = new();

    public int Count => _entries.Length;
    public Type ClassType => typeof(T);
    public string ClassName => _className;

    public TestEntryFilterData GetFilterData(int index)
    {
        var entry = _entries[index];
        return new TestEntryFilterData
        {
            MethodName = entry.MethodName,
            ClassName = _className,
            Categories = entry.Categories,
            Properties = entry.Properties,
            DependsOn = entry.DependsOn,
            HasDataSource = entry.HasDataSource,
            RepeatCount = entry.RepeatCount,
        };
    }

    public IReadOnlyList<TestMetadata> Materialize(int index, string testSessionId)
    {
        return [_entries[index].ToTestMetadata(testSessionId)];
    }
}
