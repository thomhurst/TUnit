using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions;
using TUnit.Core;

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2071:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
internal class SourceGeneratedTestsConstructor(IExtension extension, 
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector, 
    ContextManager contextManager,
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector, contextManager, serviceProvider)
{
    protected override async Task<DiscoveredTest[]> DiscoverTestsAsync()
    {
        var testMetadatas = new List<TestMetadata>();
        await foreach (var testMetadata in testsCollector.GetTestsAsync())
        {
            testMetadatas.Add(testMetadata);
        }
        
        var dynamicTests = testsCollector.GetDynamicTests();

        var discoveredTests = testMetadatas
            .Select(ConstructTest)
            .Concat(dynamicTests.SelectMany(ConstructTests))
            .ToArray();
        
        return discoveredTests;
    }
}