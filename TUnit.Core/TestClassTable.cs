using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// Groups all test methods for a single class, providing switch-based dispatch
/// that consolidates N per-method methods into 1 per-class materializer.
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public readonly struct TestClassTable
{
    /// <summary>
    /// Gets the test class type.
    /// </summary>
    public required Type ClassType { get; init; }

    /// <summary>
    /// Gets the start index of this class's entries in the global TestRegistrationEntry array.
    /// </summary>
    public required int EntryStartIndex { get; init; }

    /// <summary>
    /// Gets the number of entries for this class.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Gets the switch-based materializer that creates TestMetadata for a specific method index.
    /// JIT'd only when first test from this class is materialized (deferred from startup).
    /// </summary>
    public required Func<int, string, IReadOnlyList<TestMetadata>> Materialize { get; init; }
}
