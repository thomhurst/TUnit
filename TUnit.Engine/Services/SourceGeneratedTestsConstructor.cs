using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions;
using TUnit.Core;

namespace TUnit.Engine.Services;

[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
[SuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[SuppressMessage("Trimming", "IL2071:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[SuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[SuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[SuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
internal class SourceGeneratedTestsConstructor(IExtension extension, 
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector, 
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector, serviceProvider)
{
    protected override DiscoveredTest[] DiscoverTests()
    {
        var testMetadatas = testsCollector.GetTests();
        
        var dynamicTests = testsCollector.GetDynamicTests();

        var discoveredTests = testMetadatas
            .Select(ConstructTest)
            .Concat(dynamicTests.SelectMany(ConstructTests))
            .ToArray();
        
        return discoveredTests;
    }
}