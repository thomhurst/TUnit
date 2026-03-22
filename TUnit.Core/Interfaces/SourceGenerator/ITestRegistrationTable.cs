using System.ComponentModel;

namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Provides bulk test registration via static data tables.
/// Implemented by source-generated code (one implementation per assembly).
/// Enables O(1) JIT startup by deferring method compilation to execution time.
/// </summary>
/// <remarks>
/// <para>
/// The engine checks for this interface first when collecting tests.
/// If present, it uses the data-table fast path for filtering and
/// deferred materialization. Tests registered via <see cref="ITestSource"/>
/// (e.g., generic tests) are collected separately and merged.
/// </para>
/// </remarks>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public interface ITestRegistrationTable
{
    /// <summary>
    /// Returns all test entries in this assembly as a contiguous span.
    /// Entries are pure data (no delegates) and can be filtered without JIT.
    /// </summary>
    ReadOnlySpan<TestRegistrationEntry> GetEntries();

    /// <summary>
    /// Resolves a class type from its index in the registration table.
    /// </summary>
    /// <param name="classTypeIndex">The index into the class type array.</param>
    /// <returns>The Type for the specified class index.</returns>
    Type GetClassType(int classTypeIndex);

    /// <summary>
    /// Materializes full test metadata for a specific test.
    /// This is where JIT compilation of per-class switch methods occurs.
    /// Called only for tests that pass filtering.
    /// </summary>
    /// <param name="classTypeIndex">The class type index.</param>
    /// <param name="methodIndex">The method index within the class.</param>
    /// <param name="testSessionId">The test session identifier.</param>
    /// <returns>The materialized test metadata.</returns>
    IReadOnlyList<TestMetadata> Materialize(int classTypeIndex, int methodIndex, string testSessionId);
}
