using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// Non-generic interface for the engine to access test entries without knowing T.
/// Each TestEntrySource&lt;T&gt; implements this to provide materialization.
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public interface ITestEntrySource
{
    /// <summary>Number of test entries.</summary>
    int Count { get; }

    /// <summary>The test class type.</summary>
    Type ClassType { get; }

    /// <summary>Get the class name for filtering (from the first entry).</summary>
    string ClassName { get; }

    /// <summary>Get entry data for filtering at the given index.</summary>
    TestEntryFilterData GetFilterData(int index);

    /// <summary>Materialize a TestMetadata for the entry at the given index.</summary>
    IReadOnlyList<TestMetadata> Materialize(int index, string testSessionId);
}

/// <summary>
/// Lightweight data extracted from a TestEntry for filtering — no delegates, no JIT.
/// </summary>
public readonly struct TestEntryFilterData
{
    public required string MethodName { get; init; }
    public required string ClassName { get; init; }
    public required string[] Categories { get; init; }
    public required string[] Properties { get; init; }
    public required string[] DependsOn { get; init; }
    public required bool HasDataSource { get; init; }
    public required int RepeatCount { get; init; }
}
