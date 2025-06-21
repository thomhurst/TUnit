using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Handles registration of test sources from generated code.
/// </summary>
public static class TestSourceRegistrar
{
    /// <summary>
    /// Registers test metadata.
    /// The source generator calls this to register discovered tests.
    /// </summary>
    [RequiresDynamicCode("TestMetadataSource uses TestBuilder which requires dynamic code generation")]
    [RequiresUnreferencedCode("TestMetadataSource uses TestBuilder which may use types that aren't statically referenced")]
    public static void RegisterMetadata(IReadOnlyList<TestMetadata> testMetadata)
    {
        var metadataSource = new TestMetadataSource(testMetadata);
        Sources.TestSources.Enqueue(metadataSource);
    }
}