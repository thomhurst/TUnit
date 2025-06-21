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
        var metadataSource = new TestMetadataSource(testDescriptors);
        Sources.TestSources.Enqueue(metadataSource);
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