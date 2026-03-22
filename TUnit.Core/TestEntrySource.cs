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
    private readonly TestEntry<T>[] _entries;
    private readonly string _className;

    public TestEntrySource(TestEntry<T>[] entries)
    {
        _entries = entries;
        _className = typeof(T).Name;
    }

    public int Count => _entries.Length;
    public Type ClassType => typeof(T);
    public string ClassName => _className;

    public TestEntryFilterData GetFilterData(int index)
    {
        var entry = _entries[index];
        return new TestEntryFilterData
        {
            MethodName = entry.MethodName,
            FullyQualifiedName = entry.FullyQualifiedName,
            ClassName = _className,
            Categories = entry.Categories,
            CustomProperties = entry.CustomProperties,
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
