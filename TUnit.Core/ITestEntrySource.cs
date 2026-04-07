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
    /// <summary>The test class type.</summary>
    Type ClassType { get; }

    /// <summary>Get the class name for filtering. Same for all entries in this source.</summary>
    string ClassName { get; }

    /// <summary>
    /// Lightweight per-entry filter data. Reading this does not trigger the heavy
    /// per-class delegate/metadata <c>.cctor</c>. Callers should snapshot this reference
    /// once and loop over it, rather than calling repeatedly, to avoid any torn reads
    /// if multiple sources are registered concurrently.
    /// <para>
    /// The returned array is internal engine state — callers <b>must not</b> mutate it.
    /// It is exposed as a bare array (not <see cref="IReadOnlyList{T}"/>) purely to avoid
    /// interface-dispatch overhead on the discovery hot path.
    /// </para>
    /// </summary>
    TestEntryFilterData[] FilterData { get; }

    /// <summary>Materialize a TestMetadata for the entry at the given index.</summary>
    TestMetadata Materialize(int index, string testSessionId);
}

/// <summary>
/// Lightweight data extracted from a TestEntry for filtering — no delegates, no JIT.
/// Emitted by the source generator into a separate static class so that filtering can read it
/// without triggering the heavier <c>__TestSource..cctor</c> that builds delegates and metadata.
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public readonly struct TestEntryFilterData
{
    public required string MethodName { get; init; }
    public required string[] DependsOn { get; init; }
}
