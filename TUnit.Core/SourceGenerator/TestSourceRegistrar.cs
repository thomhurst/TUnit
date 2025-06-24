using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Handles registration of test sources from generated code.
/// </summary>
public static class TestSourceRegistrar
{
    /// <summary>
    /// Registers test descriptors.
    /// The source generator calls this to register discovered tests.
    /// </summary>
    public static void RegisterTests(IReadOnlyList<ITestDescriptor> testDescriptors)
    {
        // In the new architecture, test descriptors would be registered differently
        // For now, we'll skip this as it's part of the old architecture
        // The new architecture uses TestMetadataRegistry.RegisterSource() instead
    }
    
    /// <summary>
    /// Legacy method for backwards compatibility.
    /// New code should use RegisterTests.
    /// </summary>
    [Obsolete("Use RegisterTests instead")]
    public static void RegisterMetadata(IReadOnlyList<DynamicTestMetadata> testMetadata)
    {
        RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());
    }
}