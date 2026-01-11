namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Provides fast enumeration of test descriptors for filtering.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables two-phase test discovery:
/// </para>
/// <list type="number">
/// <item>
/// <description>Fast enumeration: <see cref="EnumerateTestDescriptors"/> returns lightweight
/// <see cref="TestDescriptor"/> instances without materializing full metadata.</description>
/// </item>
/// <item>
/// <description>Lazy materialization: Only tests that pass filtering have their
/// <see cref="TestDescriptor.Materializer"/> invoked to create full <see cref="TestMetadata"/>.</description>
/// </item>
/// </list>
/// <para>
/// Implementing this interface is optional. Classes that only implement <see cref="ITestSource"/>
/// will have a default implementation provided that materializes all tests.
/// </para>
/// </remarks>
public interface ITestDescriptorSource
{
    /// <summary>
    /// Enumerates test descriptors without materializing full test metadata.
    /// </summary>
    /// <returns>
    /// An enumerable of lightweight test descriptors that can be used for filtering.
    /// Each descriptor contains a <see cref="TestDescriptor.Materializer"/> delegate
    /// that can be invoked to create the full <see cref="TestMetadata"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method should be fast and allocation-free where possible.
    /// The returned descriptors should contain pre-computed filter hints
    /// (categories, properties) extracted at compile time.
    /// </para>
    /// <para>
    /// For parameterized tests, a single descriptor represents all data rows.
    /// The actual data rows are enumerated when <see cref="TestDescriptor.Materializer"/> is invoked.
    /// </para>
    /// </remarks>
    IEnumerable<TestDescriptor> EnumerateTestDescriptors();
}
